import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../api/client';
import type { PhotoDetailDto, PhotoPage } from './types';

export function usePhotos(params?: { q?: string; favourites?: boolean }) {
  return useInfiniteQuery<PhotoPage>({
    queryKey: ['photos', params],
    queryFn: ({ pageParam }) =>
      apiClient
        .get<PhotoPage>('/photos', {
          params: {
            cursor: pageParam,
            pageSize: 50,
            q: params?.q,
            favourites: params?.favourites || undefined,
          },
        })
        .then((r) => r.data),
    initialPageParam: undefined,
    getNextPageParam: (last) => last.nextCursor,
  });
}

export function useTrashPhotos() {
  return useInfiniteQuery<PhotoPage>({
    queryKey: ['photos', 'trash'],
    queryFn: ({ pageParam }) =>
      apiClient
        .get<PhotoPage>('/photos/trash', { params: { cursor: pageParam, pageSize: 50 } })
        .then((r) => r.data),
    initialPageParam: undefined,
    getNextPageParam: (last) => last.nextCursor,
  });
}

export function usePhoto(publicId: string) {
  return useQuery<PhotoDetailDto>({
    queryKey: ['photos', publicId],
    queryFn: () => apiClient.get<PhotoDetailDto>(`/photos/${publicId}`).then((r) => r.data),
    enabled: !!publicId,
  });
}

export function useToggleFavourite() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (publicId: string) =>
      apiClient.post<{ isFavourite: boolean }>(`/photos/${publicId}/favourite`).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['photos'] });
    },
  });
}

export function useSoftDeletePhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (publicId: string) => apiClient.delete(`/photos/${publicId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['photos'] }),
  });
}

export function useRestorePhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (publicId: string) => apiClient.post(`/photos/${publicId}/restore`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['photos'] }),
  });
}

export function usePermanentDeletePhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (publicId: string) => apiClient.delete(`/photos/${publicId}/permanent`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['photos'] }),
  });
}
