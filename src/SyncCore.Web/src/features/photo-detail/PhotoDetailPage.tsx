import { useNavigate, useParams } from 'react-router-dom';
import { usePhoto, useSoftDeletePhoto, useToggleFavourite } from '../gallery/api';
import { format } from 'date-fns';

export function PhotoDetailPage() {
  const { publicId } = useParams<{ publicId: string }>();
  const navigate = useNavigate();
  const { data: photo, isPending } = usePhoto(publicId ?? '');
  const { mutate: toggle } = useToggleFavourite();
  const { mutate: softDelete } = useSoftDeletePhoto();

  if (isPending) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  if (!photo) {
    return <p className="p-8 text-gray-500">Photo not found.</p>;
  }

  const handleDelete = () => {
    softDelete(photo.publicId, {
      onSuccess: () => navigate(-1),
    });
  };

  return (
    <div className="flex flex-col md:flex-row h-screen bg-black md:bg-white">
      {/* Image */}
      <div className="flex-1 flex items-center justify-center bg-black">
        <img
          src={photo.previewUrl}
          alt={photo.fileName}
          className="max-h-screen max-w-full object-contain"
        />
      </div>

      {/* Sidebar */}
      <div className="w-full md:w-80 bg-white p-6 flex flex-col gap-4 overflow-y-auto">
        <button
          onClick={() => navigate(-1)}
          className="self-start text-sm text-gray-500 hover:text-gray-800"
        >
          ← Back
        </button>

        <h2 className="text-lg font-semibold text-gray-800 break-all">{photo.fileName}</h2>

        <dl className="text-sm space-y-2 text-gray-600">
          <div>
            <dt className="font-medium">Taken</dt>
            <dd>{format(new Date(photo.takenAt), 'PPp')}</dd>
          </div>
          <div>
            <dt className="font-medium">Uploaded</dt>
            <dd>{format(new Date(photo.uploadedAt), 'PPp')}</dd>
          </div>
          <div>
            <dt className="font-medium">Size</dt>
            <dd>{(photo.fileSizeBytes / 1024 / 1024).toFixed(2)} MB</dd>
          </div>
          <div>
            <dt className="font-medium">Type</dt>
            <dd>{photo.contentType}</dd>
          </div>
        </dl>

        <div className="flex gap-2 mt-auto">
          <button
            onClick={() => toggle(photo.publicId)}
            className="flex-1 rounded-lg border border-gray-200 py-2 text-sm hover:bg-gray-50"
          >
            {photo.isFavourite ? '★ Favourited' : '☆ Favourite'}
          </button>
          <a
            href={photo.originalUrl}
            download={photo.fileName}
            className="flex-1 rounded-lg border border-gray-200 py-2 text-sm text-center hover:bg-gray-50"
          >
            Download
          </a>
        </div>

        <button
          onClick={handleDelete}
          className="mt-2 rounded-lg bg-red-50 py-2 text-sm text-red-600 hover:bg-red-100"
        >
          Move to Trash
        </button>
      </div>
    </div>
  );
}
