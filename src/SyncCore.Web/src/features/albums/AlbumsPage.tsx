import { useState } from 'react';
import { useAlbums, useCreateAlbum, useDeleteAlbum } from './api';
import { Link } from 'react-router-dom';

export function AlbumsPage() {
  const { data: albums, isPending } = useAlbums();
  const { mutate: createAlbum } = useCreateAlbum();
  const { mutate: deleteAlbum } = useDeleteAlbum();
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');

  const handleCreate = () => {
    if (!newName.trim()) return;
    createAlbum(newName.trim(), {
      onSuccess: () => { setNewName(''); setShowCreate(false); },
    });
  };

  if (isPending) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  return (
    <div className="p-4 flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-gray-800">Albums</h1>
        <button
          onClick={() => setShowCreate(true)}
          className="rounded-full bg-blue-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-blue-700"
        >
          New album
        </button>
      </div>

      {showCreate && (
        <div className="flex gap-2 items-center">
          <input
            autoFocus
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') handleCreate(); if (e.key === 'Escape') setShowCreate(false); }}
            placeholder="Album name"
            className="flex-1 rounded-lg border border-gray-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          />
          <button onClick={handleCreate} className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700">
            Create
          </button>
          <button onClick={() => setShowCreate(false)} className="rounded-lg border px-4 py-2 text-sm hover:bg-gray-50">
            Cancel
          </button>
        </div>
      )}

      {(albums?.length ?? 0) === 0 ? (
        <p className="text-gray-500 mt-8 text-center">No albums yet.</p>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
          {albums?.map((album) => (
            <div key={album.publicId} className="group relative rounded-xl overflow-hidden border border-gray-100 hover:shadow-md transition-shadow">
              <Link to={`/albums/${album.publicId}`}>
                {album.coverThumbUrl ? (
                  <img src={album.coverThumbUrl} alt={album.name} className="w-full aspect-square object-cover" />
                ) : (
                  <div className="w-full aspect-square bg-gray-100 flex items-center justify-center text-gray-400 text-4xl">📷</div>
                )}
                <div className="p-3">
                  <p className="font-medium text-gray-800 truncate">{album.name}</p>
                  <p className="text-xs text-gray-500">{album.photoCount} photos</p>
                </div>
              </Link>
              <button
                onClick={() => deleteAlbum(album.publicId)}
                className="absolute top-2 right-2 hidden group-hover:flex items-center justify-center w-7 h-7 rounded-full bg-black/50 text-white text-xs hover:bg-black/70"
                title="Delete album"
              >
                ✕
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
