import axios from 'axios';

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string) || '';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

/** Prefix relative API paths (like /photos/id/thumb) with the API base URL for use in <img src>. */
export function apiImageUrl(path: string): string {
  if (!path) return '';
  if (path.startsWith('http')) return path; // already absolute (SAS URL fallback)
  return `${API_BASE_URL}${path}`;
}
