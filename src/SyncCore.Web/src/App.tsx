import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { MsalProvider } from '@azure/msal-react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { msalInstance } from './auth/msalInstance';
import { RequireAuth } from './auth/RequireAuth';
import { AppLayout } from './components/AppLayout';
import { GalleryPage } from './features/gallery/GalleryPage';
import { PhotoDetailPage } from './features/photo-detail/PhotoDetailPage';
import { AlbumsPage } from './features/albums/AlbumsPage';
import { AlbumDetailPage } from './features/albums/AlbumDetailPage';
import { TrashPage } from './features/trash/TrashPage';
import { DropZone } from './features/upload/DropZone';

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 1000 * 60 } },
});

const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <RequireAuth>
        <DropZone>
          <AppLayout />
        </DropZone>
      </RequireAuth>
    ),
    children: [
      { index: true, element: <GalleryPage /> },
      { path: 'favourites', element: <GalleryPage favouritesOnly /> },
      { path: 'albums', element: <AlbumsPage /> },
      { path: 'albums/:albumId', element: <AlbumDetailPage /> },
      { path: 'trash', element: <TrashPage /> },
      { path: 'photos/:publicId', element: <PhotoDetailPage /> },
    ],
  },
]);

export default function App() {
  return (
    <MsalProvider instance={msalInstance}>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </MsalProvider>
  );
}
