import { useRef, useCallback } from 'react';
import { usePhotos } from './api';
import { PhotoThumbnail } from './PhotoThumbnail';
import { UploadButton } from '../upload/UploadButton';

interface Props {
  favouritesOnly?: boolean;
  searchTerm?: string;
}

export function GalleryPage({ favouritesOnly, searchTerm }: Props) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isPending } = usePhotos({
    favourites: favouritesOnly,
    q: searchTerm,
  });

  const observer = useRef<IntersectionObserver | null>(null);
  const sentinelRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (isFetchingNextPage) return;
      if (observer.current) observer.current.disconnect();
      if (node) {
        observer.current = new IntersectionObserver((entries) => {
          if (entries[0].isIntersecting && hasNextPage) fetchNextPage();
        });
        observer.current.observe(node);
      }
    },
    [isFetchingNextPage, hasNextPage, fetchNextPage],
  );

  const photos = data?.pages.flatMap((p) => p.items) ?? [];

  if (isPending) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-gray-800">
          {favouritesOnly ? 'Favourites' : 'Photos'}
        </h1>
        {!favouritesOnly && <UploadButton />}
      </div>

      {photos.length === 0 ? (
        <p className="text-gray-500 mt-8 text-center">No photos yet. Upload some!</p>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-1">
          {photos.map((photo) => (
            <PhotoThumbnail key={photo.publicId} photo={photo} />
          ))}
        </div>
      )}

      <div ref={sentinelRef} className="h-4" />
      {isFetchingNextPage && (
        <div className="flex justify-center py-4">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600" />
        </div>
      )}
    </div>
  );
}
