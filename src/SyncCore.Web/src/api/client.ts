import axios from 'axios';
import { msalInstance } from '../auth/msalInstance';
import { loginRequest } from '../auth/msalConfig';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL as string,
});

apiClient.interceptors.request.use(async (config) => {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) return config;

  const response = await msalInstance.acquireTokenSilent({
    ...loginRequest,
    account: accounts[0],
  });

  config.headers.Authorization = `Bearer ${response.accessToken}`;
  return config;
});
