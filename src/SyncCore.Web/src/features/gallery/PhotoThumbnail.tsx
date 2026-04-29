import { Link } from 'react-router-dom';
import type { PhotoDto } from './types';
import { useToggleFavourite } from './api';
import { apiImageUrl } from '../../api/client';

interface Props {
  photo: PhotoDto;
  selected?: boolean;
  selecting?: boolean;
  onToggleSelect?: (publicId: string) => void;
}

export function PhotoThumbnail({ photo, selected = false, selecting = false, onToggleSelect }: Props) {
  const { mutate: toggle } = useToggleFavourite();

  const handleClick = (e: React.MouseEvent) => {
    if (selecting) {
      e.preventDefault();
      onToggleSelect?.(photo.publicId);
    }
  };

  return (
    <div
      className={`relative group h-full w-full overflow-hidden bg-gray-100 cursor-pointer select-none ${selected ? 'ring-2 ring-inset ring-[#1a73e8]' : ''}`}
      onClick={handleClick}
    >
      {/* Tinted overlay when selected */}
      {selected && <div className="absolute inset-0 bg-[#1a73e8]/20 z-10 pointer-events-none" />}

      {/* Checkbox — shown on hover or when any item is selected */}
      <div
        className={`absolute top-2 left-2 z-20 transition-opacity ${selecting || selected ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}
        onClick={(e) => { e.preventDefault(); e.stopPropagation(); onToggleSelect?.(photo.publicId); }}
      >
        <div className={`w-6 h-6 rounded-full border-2 flex items-center justify-center transition-colors ${selected ? 'bg-[#1a73e8] border-[#1a73e8]' : 'bg-black/40 border-white'}`}>
          {selected && (
            <svg viewBox="0 0 24 24" fill="white" className="w-4 h-4">
              <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
            </svg>
          )}
        </div>
      </div>

      <Link to={`/photos/${photo.publicId}`} onClick={(e) => { if (selecting) e.preventDefault(); }}>
        <img
          src={apiImageUrl(photo.thumbUrl)}
          alt={photo.fileName}
          className="w-full h-full object-cover transition-opacity group-hover:opacity-90"
          loading="lazy"
        />
      </Link>

      {/* Favourite button — hidden when selecting */}
      {!selecting && (
        <button
          onClick={(e) => { e.stopPropagation(); toggle(photo.publicId); }}
          className="absolute top-1.5 right-1.5 p-1 rounded-full bg-black/40 text-white opacity-0 group-hover:opacity-100 transition-opacity z-20"
          title={photo.isFavourite ? 'Remove from favourites' : 'Add to favourites'}
        >
          {photo.isFavourite ? (
            <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
              <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
            </svg>
          ) : (
            <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
              <path d="M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5c2 0 3.5 1.5 3.5 3.5 0 2.89-3.14 5.74-7.9 10.05z" />
            </svg>
          )}
        </button>
      )}
    </div>
  );
}
