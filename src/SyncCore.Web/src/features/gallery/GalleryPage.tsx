import { useRef, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { usePhotos } from './api';
import { PhotoThumbnail } from './PhotoThumbnail';
import { UploadButton } from '../upload/UploadButton';

interface Props {
  favouritesOnly?: boolean;
}

function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center h-full min-h-[70vh] text-center px-4">
      {/* Illustration */}
      <div className="relative mb-6">
        {/* Cloud */}
        <svg viewBox="0 0 240 160" className="w-64 h-44 select-none" xmlns="http://www.w3.org/2000/svg">
          {/* back cloud */}
          <ellipse cx="170" cy="68" rx="58" ry="42" fill="#e8eaed" />
          <ellipse cx="130" cy="80" rx="75" ry="52" fill="#e8eaed" />
          {/* front cloud */}
          <ellipse cx="100" cy="90" rx="65" ry="44" fill="#f1f3f4" />
          <ellipse cx="60" cy="100" rx="48" ry="36" fill="#f1f3f4" />
          {/* yellow blob */}
          <circle cx="185" cy="45" r="22" fill="#FBBC05" />
          {/* laptop screen */}
          <rect x="50" y="78" width="90" height="60" rx="4" fill="#fff" stroke="#dadce0" strokeWidth="2" />
          <rect x="55" y="83" width="80" height="48" rx="2" fill="#f8f9fa" />
          {/* pinwheel icon */}
          <g transform="translate(95,107)">
            <circle cx="0" cy="-12" r="7" fill="#EA4335" />
            <circle cx="10.4" cy="6" r="7" fill="#FBBC05" />
            <circle cx="-10.4" cy="6" r="7" fill="#34A853" />
            <circle cx="0" cy="0" r="6" fill="#4285F4" />
          </g>
          {/* laptop base */}
          <rect x="40" y="138" width="110" height="6" rx="3" fill="#dadce0" />
          {/* stacked pages */}
          <rect x="148" y="95" width="48" height="58" rx="3" fill="#4285F4" transform="rotate(-8,148,95)" />
          <rect x="152" y="95" width="48" height="58" rx="3" fill="#5a9cf8" transform="rotate(-4,152,95)" />
          <rect x="155" y="96" width="48" height="58" rx="3" fill="#6aabff" />
        </svg>
      </div>
      <h2 className="text-2xl text-gray-700 font-normal mb-2">Ready to add some photos?</h2>
      <p className="text-sm text-gray-500 mb-6">Drag photos &amp; videos anywhere to upload</p>
      <UploadButton />
    </div>
  );
}

export function GalleryPage({ favouritesOnly }: Props) {
  const [searchParams] = useSearchParams();
  const searchTerm = searchParams.get('q') ?? undefined;

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
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500" />
      </div>
    );
  }

  if (photos.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="px-4 pt-2 pb-8">
      {searchTerm && (
        <p className="text-sm text-gray-500 mb-3">
          Results for <span className="font-medium text-gray-700">"{searchTerm}"</span>
        </p>
      )}

      {/* Photo grid — tight gaps like Google Photos */}
      <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7 gap-0.5">
        {photos.map((photo) => (
          <PhotoThumbnail key={photo.publicId} photo={photo} />
        ))}
      </div>

      <div ref={sentinelRef} className="h-4" />
      {isFetchingNextPage && (
        <div className="flex justify-center py-4">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500" />
        </div>
      )}
    </div>
  );
}
