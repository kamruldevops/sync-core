import { useParams } from 'react-router-dom';
import { useAlbumPhotos } from './api';
import { PhotoThumbnail } from '../gallery/PhotoThumbnail';

export function AlbumDetailPage() {
  const { albumId } = useParams<{ albumId: string }>();
  const { data, isPending } = useAlbumPhotos(albumId ?? '');

  if (isPending) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  const photos = data?.items ?? [];

  return (
    <div className="p-4 flex flex-col gap-4">
      <h1 className="text-2xl font-semibold text-gray-800">Album</h1>
      {photos.length === 0 ? (
        <p className="text-gray-500 mt-8 text-center">No photos in this album.</p>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-1">
          {photos.map((photo) => (
            <PhotoThumbnail key={photo.publicId} photo={photo} />
          ))}
        </div>
      )}
    </div>
  );
}
