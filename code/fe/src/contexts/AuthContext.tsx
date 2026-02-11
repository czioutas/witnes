import React, { createContext, useContext, useReducer, useEffect } from "react";
import { getWitnesServerAPI, AccountRoles } from "../generated/api";

import type {
  AuthState,
  AuthContextType,
  LoginCredentials,
  RegisterData,
  User,
  AuthTokens,
} from "../types/auth";

const api = getWitnesServerAPI();

// Token management functions
const getTokens = (): AuthTokens | null => {
  if (typeof window === "undefined") return null;
  const stored = localStorage.getItem("auth_tokens");
  if (stored) {
    return JSON.parse(stored);
  }
  return null;
};

const setTokens = (tokens: AuthTokens) => {
  if (typeof window === "undefined") return;
  localStorage.setItem("auth_tokens", JSON.stringify(tokens));
};

const clearTokens = () => {
  if (typeof window === "undefined") return;
  localStorage.removeItem("auth_tokens");
};

const decodeToken = (token: string): any => {
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

const isTokenExpired = (token: string): boolean => {
  if (!token) return true;
  const decoded = decodeToken(token);
  if (!decoded || !decoded.exp) {
    return true;
  }
  const now = Date.now() / 1000;
  return decoded.exp < now;
};

// Initial state - must be identical on server and client for hydration
const initialState: AuthState = {
  user: null,
  tokens: null,
  isAuthenticated: false,
  isLoading: false, // Start as false to prevent hydration mismatch
  error: null,
};

// Action types
type AuthAction =
  | { type: "AUTH_START" }
  | { type: "AUTH_SUCCESS"; payload: { user: User; tokens: AuthTokens } }
  | { type: "AUTH_ERROR"; payload: string }
  | { type: "AUTH_LOGOUT" }
  | { type: "CLEAR_ERROR" }
  | { type: "SET_LOADING"; payload: boolean };

// Reducer
function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case "AUTH_START":
      return {
        ...state,
        isLoading: true,
        error: null,
      };

    case "AUTH_SUCCESS":
      return {
        ...state,
        user: action.payload.user,
        tokens: action.payload.tokens,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      };

    case "AUTH_ERROR":
      return {
        ...state,
        user: null,
        tokens: null,
        isAuthenticated: false,
        isLoading: false,
        error: action.payload,
      };

    case "AUTH_LOGOUT":
      return {
        ...initialState,
        isLoading: false,
      };

    case "CLEAR_ERROR":
      return {
        ...state,
        error: null,
      };

    case "SET_LOADING":
      return {
        ...state,
        isLoading: action.payload,
      };

    default:
      return state;
  }
}

// Create context
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Provider props
interface AuthProviderProps {
  children: React.ReactNode;
}

/**
 * Initialize auth state synchronously on first render (client-side only)
 */
function initAuthState(initial: AuthState): AuthState {
  // On server, return initial state as-is (isLoading: false to match client initial render)
  if (typeof window === "undefined") {
    return initial;
  }

  try {
    const tokens = getTokens();

    // No tokens = not authenticated
    if (!tokens) {
      return { ...initial, isLoading: false };
    }

    // Check if token is valid
    if (!isTokenExpired(tokens.accessToken)) {
      const decoded = decodeToken(tokens.accessToken);
      if (decoded) {
        const user: User = {
          id: decoded.sub || "",
          email: decoded.email || "",
          firstName: decoded.firstName || decoded.given_name || "",
          lastName: decoded.lastName || decoded.family_name || "",
          company: decoded.company || decoded.tenant_name || "",
          limited: decoded.limited || false,
          roles:
            decoded[
              "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            ] || [],
        };

        return {
          user,
          tokens,
          isAuthenticated: true,
          isLoading: false,
          error: null,
        };
      }
    }

    // Token exists but needs refresh - start loading after hydration
    return { ...initial, isLoading: false };
  } catch (error) {
    console.error("Error initializing auth state:", error);
    return { ...initial, isLoading: false };
  }
}

// Auth Provider component
export function AuthProvider({ children }: AuthProviderProps) {
  const [state, dispatch] = useReducer(
    authReducer,
    initialState,
    initAuthState,
  );

  // Check if tokens need refresh (async operation)
  useEffect(() => {
    handleTokenRefresh();
  }, []);

  /**
   * Handle token refresh if needed (runs after mount for expired tokens)
   */
  const handleTokenRefresh = async () => {
    // Only run in browser
    if (typeof window === "undefined") return;

    const tokens = getTokens();
    if (!tokens) return;

    // If already authenticated with valid token, optionally fetch fresh data
    if (state.isAuthenticated && !isTokenExpired(tokens.accessToken)) {
      getCurrentUser().catch((err) => {
        console.warn("Background user fetch failed:", err);
      });
      return;
    }

    // If access token expired but refresh token is valid, refresh it
    if (
      isTokenExpired(tokens.accessToken) &&
      !isTokenExpired(tokens.refreshToken)
    ) {
      dispatch({ type: "SET_LOADING", payload: true });
      try {
        const response = await api.getV1AccountRefreshTokenRefreshToken(
          tokens.refreshToken,
        );
        const tokenData = response.data;
        const newTokens: AuthTokens = {
          accessToken: tokenData.access_token || "",
          refreshToken: tokenData.refresh_token || "",
        };
        setTokens(newTokens);
        await getCurrentUser();
      } catch (error) {
        console.error("Token refresh failed:", error);
        clearTokens();
        dispatch({ type: "AUTH_LOGOUT" });
      }
    }
  };

  /**
   * Get current user info using generated client
   */
  const getCurrentUser = async () => {
    try {
      const response = await api.getV1AccountSelf();
      const userData = response.data;

      // Map from generated API response to our User type
      const user: User = {
        id: userData.id || "",
        email: userData.email || "",
        firstName: userData.first_name || "",
        lastName: userData.last_name || "",
        company: userData.tenant_name || "",
        limited: userData.limited || false,
        roles: userData.roles || [],
      };

      const tokens = getTokens();

      if (tokens) {
        dispatch({
          type: "AUTH_SUCCESS",
          payload: { user, tokens },
        });

        // Identify user with Witnes
        if (typeof window !== "undefined") {
          if (window.Witnes) {
            window.Witnes.identify(user.id);
            console.log("Witnes identified user:", user.id);
          } else {
            // If script hasn't loaded yet, queue it in the config
            window.witnesConfig = window.witnesConfig || { projectKey: "" };
            window.witnesConfig.userId = user.id;
          }
        }
      }
    } catch (error) {
      console.error("Get current user error:", error);
      // If the self endpoint fails, clear tokens and log out the user
      clearTokens();
      dispatch({ type: "AUTH_LOGOUT" });
    }
  };

  /**
   * Login function
   */
  const login = async (credentials: LoginCredentials) => {
    dispatch({ type: "AUTH_START" });

    try {
      const response = await api.postV1AccountLogin({
        email: credentials.email,
        password: credentials.password,
      });

      const tokenData = response.data;
      const tokens: AuthTokens = {
        accessToken: tokenData.access_token || "",
        refreshToken: tokenData.refresh_token || "",
      };

      // Set tokens in API client
      setTokens(tokens);

      // Get user info after login
      await getCurrentUser();
    } catch (error: any) {
      const errorMessage =
        error?.status === 401 || error?.response?.status === 401
          ? "Invalid email or password"
          : "Login failed. Please try again.";

      dispatch({
        type: "AUTH_ERROR",
        payload: errorMessage,
      });

      // Re-throw the error so calling code knows login failed
      throw error;
    }
  };

  /**
   * Register function
   * Does NOT store tokens — user must verify email first, then log in.
   */
  const register = async (data: RegisterData) => {
    dispatch({ type: "AUTH_START" });

    try {
      await api.postV1AccountRegister({
        email: data.email,
        password: data.password,
        first_name: data.firstName,
        last_name: data.lastName,
        normalized_tenant_identifier: data.company,
      });

      // Registration succeeded — don't store tokens.
      // The user needs to verify their email before they can log in.
      dispatch({ type: "SET_LOADING", payload: false });
    } catch (error: any) {
      const errorMessage =
        error?.status === 400 || error?.response?.status === 400
          ? "Invalid registration data. Please check your information."
          : "Registration failed. Please try again.";

      dispatch({
        type: "AUTH_ERROR",
        payload: errorMessage,
      });

      // Re-throw the error so calling code knows registration failed
      throw error;
    }
  };

  /**
   * Register from invitation
   */
  const registerInvitation = async (data: {
    invitationCode: string;
    password: string;
  }) => {
    dispatch({ type: "AUTH_START" });

    try {
      const response = await api.postV1AccountRegisterInvitation({
        invitation_code: data.invitationCode,
        password: data.password,
      });

      const tokenData = response.data;

      const tokens: AuthTokens = {
        accessToken: tokenData.access_token || "",
        refreshToken: tokenData.refresh_token || "",
      };

      // Set tokens in API client
      setTokens(tokens);

      // Get user info after registration
      await getCurrentUser();
    } catch (error: any) {
      const errorMessage =
        error?.status === 400 || error?.response?.status === 400
          ? "Invalid invitation code or the invitation has expired."
          : "Registration failed. Please try again.";

      dispatch({
        type: "AUTH_ERROR",
        payload: errorMessage,
      });

      // Re-throw the error so calling code knows registration failed
      throw error;
    }
  };

  /**
   * Logout function
   */
  const logout = async () => {
    // Clear tokens and state regardless of API call result
    clearTokens();
    dispatch({ type: "AUTH_LOGOUT" });
  };

  /**
   * Refresh token function
   */
  const refreshToken = async () => {
    try {
      await handleTokenRefresh();
      // Get updated user info after token refresh
      await getCurrentUser();
    } catch (error) {
      console.error("Token refresh failed:", error);
      // Refresh failed, logout user
      await logout();
    }
  };

  /**
   * Clear error function
   */
  const clearError = () => {
    dispatch({ type: "CLEAR_ERROR" });
  };

  // Context value
  const contextValue: AuthContextType = {
    ...state,
    login,
    register,
    registerInvitation,
    logout,
    refreshToken,
    clearError,
  };

  return (
    <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>
  );
}

// Hook to use auth context
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

// Hook to require authentication
export function useRequireAuth(): AuthContextType {
  const auth = useAuth();

  useEffect(() => {
    // Only redirect if we're in browser, not loading, and not authenticated
    if (
      typeof window !== "undefined" &&
      !auth.isLoading &&
      !auth.isAuthenticated
    ) {
      const currentPath = window.location.pathname;
      // Avoid redirect loop by checking if we're already on login page
      if (currentPath !== "/authenticate/login") {
        window.location.href = `/authenticate/login?redirect=${encodeURIComponent(
          currentPath,
        )}`;
      }
    }
  }, [auth.isAuthenticated, auth.isLoading]);

  return auth;
}

// Helper function to check if user has a specific role
export function hasRole(user: User | null, role: AccountRoles): boolean {
  if (!user || !user.roles) return false;
  // Check if roles is an array
  if (Array.isArray(user.roles)) {
    return user.roles.includes(role);
  }
  // If roles is a single value
  return user.roles === role;
}

// Hook to require specific role
export function useRequireRole(requiredRole: AccountRoles): AuthContextType {
  const auth = useAuth();

  useEffect(() => {
    // Only check role if we're in browser, not loading, and authenticated
    if (
      typeof window !== "undefined" &&
      !auth.isLoading &&
      auth.isAuthenticated
    ) {
      if (!hasRole(auth.user, requiredRole)) {
        // Redirect to dashboard if user doesn't have required role
        window.location.href = "/dashboard";
      }
    }
  }, [auth.isAuthenticated, auth.isLoading, auth.user, requiredRole]);

  return auth;
}
