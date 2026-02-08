import type { AccountRoles } from "../generated/api";

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  company?: string;
  avatar?: string;
  limited: boolean;
  roles: AccountRoles[];
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface AuthState {
  user: User | null;
  tokens: AuthTokens | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterData {
  firstName: string;
  lastName: string;
  company: string;
  email: string;
  password: string;
  joinCode?: string | null;
}

export interface AuthContextType extends AuthState {
  login: (credentials: LoginCredentials) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  registerInvitation: (data: { invitationCode: string; password: string }) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  clearError: () => void;
}

export interface ApiError {
  message: string;
  status: number;
  code?: string;
}