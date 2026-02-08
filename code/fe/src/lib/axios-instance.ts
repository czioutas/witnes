// src/lib/axios-instance.ts
import axios, { type AxiosRequestConfig, type AxiosResponse } from 'axios';
import type { AuthTokens } from '../types/auth';
import { decodeToken } from './auth'; // your decodeToken function
import { getBaseUrl } from './utils';

const BASE_URL = getBaseUrl();

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: AxiosResponse<any>) => void;
  reject: (error: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error);
    else resolve(token as any);
  });
  failedQueue = [];
};

const getTokens = (): AuthTokens | null => {
  if (typeof window === 'undefined') return null;
  const stored = localStorage.getItem('auth_tokens');
  return stored ? JSON.parse(stored) : null;
};

const setTokens = (tokens: AuthTokens) => {
  if (typeof window === 'undefined') return;
  localStorage.setItem('auth_tokens', JSON.stringify(tokens));
};

const clearTokens = () => {
  if (typeof window === 'undefined') return;
  localStorage.removeItem('auth_tokens');
};

const isTokenExpired = (token: string) => {
  if (!token) return true;
  const decoded = decodeToken(token);
  if (!decoded?.exp) return true;
  return decoded.exp < Date.now() / 1000;
};

// Function to refresh tokens
const refreshTokens = async (): Promise<AuthTokens> => {
  const tokens = getTokens();
  if (!tokens?.refreshToken) throw new Error('No refresh token');

  // If refresh token is expired, clear and throw
  if (isTokenExpired(tokens.refreshToken)) {
    clearTokens();
    throw new Error('Refresh token expired');
  }

  const response = await axios.get<AuthTokens>(`${BASE_URL}/v1/account/refresh-token/${tokens.refreshToken}`);
  const newTokens = response.data;
  setTokens(newTokens);
  return newTokens;
};

// Orval mutator
export const customInstance = async <T = any>(
  config: AxiosRequestConfig
): Promise<AxiosResponse<T>> => {
  let tokens = getTokens();
  const accessToken = tokens?.accessToken;

  const headers = {
    ...config.headers,
    ...(accessToken && !isTokenExpired(accessToken)
      ? { Authorization: `Bearer ${accessToken}` }
      : {}),
  };

  try {
    return await axios({
      ...config,
      baseURL: BASE_URL,
      headers,
    });
  } catch (err: any) {
    if (err.response?.status === 401 && tokens?.refreshToken) {
      if (!isRefreshing) {
        isRefreshing = true;
        try {
          const newTokens = await refreshTokens();
          tokens = newTokens;
          isRefreshing = false;
          processQueue(null, newTokens.accessToken);
        } catch (refreshError) {
          isRefreshing = false;
          clearTokens();
          processQueue(refreshError, null);
          throw refreshError;
        }
      }

      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: async () => {
            const newTokens = getTokens();
            if (!newTokens) return reject(err);

            try {
              const retryResponse = await axios({
                ...config,
                baseURL: BASE_URL,
                headers: {
                  ...config.headers,
                  Authorization: `Bearer ${newTokens.accessToken}`,
                },
              });
              resolve(retryResponse);
            } catch (retryError) {
              reject(retryError);
            }
          },
          reject,
        });
      });
    }

    throw err;
  }
};
