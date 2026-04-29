import { useRef, useCallback } from 'react';
import { useTrashPhotos, useRestorePhoto, usePermanentDeletePhoto } from '../gallery/api';
import { format } from 'date-fns';

export function TrashPage() {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isPending } = useTrashPhotos();
  const { mutate: restore } = useRestorePhoto();
  const { mutate: permDelete } = usePermanentDeletePhoto();

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
    <div className="p-4 flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-gray-800">Trash</h1>
        <p className="text-sm text-gray-500 mt-1">Photos are permanently deleted after 30 days.</p>
      </div>

      {photos.length === 0 ? (
        <p className="text-gray-500 mt-8 text-center">Trash is empty.</p>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-1">
          {photos.map((photo) => (
            <div key={photo.publicId} className="relative group aspect-square overflow-hidden rounded-md bg-gray-100">
              <img
                src={photo.thumbUrl}
                alt={photo.fileName}
                className="w-full h-full object-cover opacity-60"
                loading="lazy"
              />
              <div className="absolute inset-0 flex flex-col items-center justify-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity bg-black/30">
                <button
                  onClick={() => restore(photo.publicId)}
                  className="rounded-full bg-white px-3 py-1 text-xs font-medium text-gray-800 hover:bg-gray-100"
                >
                  Restore
                </button>
                <button
                  onClick={() => permDelete(photo.publicId)}
                  className="rounded-full bg-red-600 px-3 py-1 text-xs font-medium text-white hover:bg-red-700"
                >
                  Delete
                </button>
              </div>
              {photo.deletedAt && (
                <p className="absolute bottom-0 left-0 right-0 bg-black/50 text-white text-xs text-center py-0.5">
                  {format(new Date(photo.deletedAt), 'MMM d')}
                </p>
              )}
            </div>
          ))}
        </div>
      )}

      <div ref={sentinelRef} className="h-4" />
    </div>
  );
}
