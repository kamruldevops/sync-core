import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../api/client';
import type { AlbumDto } from './types';
import type { PhotoPage } from '../gallery/types';

export function useAlbums() {
  return useQuery<AlbumDto[]>({
    queryKey: ['albums'],
    queryFn: () => apiClient.get<AlbumDto[]>('/albums').then((r) => r.data),
  });
}

export function useAlbumPhotos(albumId: string) {
  return useQuery<PhotoPage>({
    queryKey: ['albums', albumId, 'photos'],
    queryFn: () => apiClient.get<PhotoPage>(`/albums/${albumId}/photos`).then((r) => r.data),
    enabled: !!albumId,
  });
}

export function useCreateAlbum() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (name: string) =>
      apiClient.post<AlbumDto>('/albums', { name }).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['albums'] }),
  });
}

export function useAddPhotoToAlbum() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ albumId, photoPublicId }: { albumId: string; photoPublicId: string }) =>
      apiClient.post(`/albums/${albumId}/photos`, { photoPublicId }),
    onSuccess: (_data, { albumId }) => qc.invalidateQueries({ queryKey: ['albums', albumId] }),
  });
}

export function useRemovePhotoFromAlbum() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ albumId, photoPublicId }: { albumId: string; photoPublicId: string }) =>
      apiClient.delete(`/albums/${albumId}/photos/${photoPublicId}`),
    onSuccess: (_data, { albumId }) => qc.invalidateQueries({ queryKey: ['albums', albumId] }),
  });
}

export function useDeleteAlbum() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (albumId: string) => apiClient.delete(`/albums/${albumId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['albums'] }),
  });
}
