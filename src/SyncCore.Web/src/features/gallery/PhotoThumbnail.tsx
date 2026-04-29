import { Link } from 'react-router-dom';
import type { PhotoDto } from './types';
import { useToggleFavourite } from './api';

interface Props {
  photo: PhotoDto;
  onSelect?: (publicId: string) => void;
}

export function PhotoThumbnail({ photo, onSelect }: Props) {
  const { mutate: toggle } = useToggleFavourite();

  return (
    <div className="relative group aspect-square overflow-hidden rounded-md bg-gray-100 cursor-pointer">
      <Link to={`/photos/${photo.publicId}`} onClick={() => onSelect?.(photo.publicId)}>
        <img
          src={photo.thumbUrl}
          alt={photo.fileName}
          className="w-full h-full object-cover transition-transform group-hover:scale-105"
          loading="lazy"
        />
      </Link>
      <button
        onClick={(e) => { e.stopPropagation(); toggle(photo.publicId); }}
        className="absolute top-1.5 right-1.5 p-1 rounded-full bg-black/40 text-white opacity-0 group-hover:opacity-100 transition-opacity"
        title={photo.isFavourite ? 'Remove from favourites' : 'Add to favourites'}
      >
        {photo.isFavourite ? '★' : '☆'}
      </button>
    </div>
  );
}
