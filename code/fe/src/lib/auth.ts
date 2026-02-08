import type {
  AuthTokens,
} from "../types/auth";

export const getTokens = (): AuthTokens | null => {
  if (typeof window === "undefined") return null;
  const stored = localStorage.getItem("auth_tokens");
  if (stored) {
    return JSON.parse(stored);
  }
  return null;
};

export const setTokens = (tokens: AuthTokens) => {
  if (typeof window === "undefined") return;
  localStorage.setItem("auth_tokens", JSON.stringify(tokens));
};

export const clearTokens = () => {
  if (typeof window === "undefined") return;
  localStorage.removeItem("auth_tokens");
};

export const decodeToken = (token: string): any => {
  try {
    if (!token || typeof token !== "string") {
      return null;
    }
    const parts = token.split(".");
    if (parts.length !== 3) {
      console.warn("Invalid JWT format");
      return null;
    }
    const payload = parts[1];
    const decoded = atob(payload);
    return JSON.parse(decoded);
  } catch (error) {
    console.error("Error decoding token:", error);
    return null;
  }
};

export const isTokenExpired = (token: string): boolean => {
  if (!token) return true;
  const decoded = decodeToken(token);
  if (!decoded || !decoded.exp) {
    return true;
  }
  const now = Date.now() / 1000;
  return decoded.exp < now;
};