import { useCallback, useState } from 'react';
import { useUploadPhoto } from './api';

export function DropZone({ children }: { children: React.ReactNode }) {
  const [dragging, setDragging] = useState(false);
  const { mutateAsync } = useUploadPhoto();

  const onDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragging(false);
      const files = e.dataTransfer.files;
      if (files.length > 0) {
        Promise.all(Array.from(files).map((f) => mutateAsync(f)));
      }
    },
    [mutateAsync],
  );

  return (
    <div
      onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onDrop={onDrop}
      className={`min-h-screen transition-colors ${dragging ? 'bg-blue-50' : ''}`}
    >
      {dragging && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-blue-100/80 pointer-events-none">
          <p className="text-xl font-semibold text-blue-700">Drop photos to upload</p>
        </div>
      )}
      {children}
    </div>
  );
}
