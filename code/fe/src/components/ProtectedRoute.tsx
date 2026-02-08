import React from 'react';
import { useAuth, useRequireAuth } from '../contexts/AuthContext';
import { Loader2 } from 'lucide-react';

interface ProtectedRouteProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  requireAuth?: boolean;
}

/**
 * ProtectedRoute component that ensures user is authenticated before rendering children
 */
export function ProtectedRoute({ 
  children, 
  fallback, 
  requireAuth = true 
}: ProtectedRouteProps) {
  const auth = useRequireAuth();

  // If auth is not required, just render children
  if (!requireAuth) {
    return <>{children}</>;
  }

  // Show loading state while checking authentication
  if (auth.isLoading) {
    return (
      fallback || (
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center space-y-4">
            <Loader2 className="h-8 w-8 animate-spin mx-auto text-primary" />
            <p className="text-muted-foreground">Loading...</p>
          </div>
        </div>
      )
    );
  }

  // Show error state if there's an auth error
  if (auth.error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center space-y-4 max-w-md">
          <div className="rounded-full bg-red-100 p-4 w-16 h-16 mx-auto flex items-center justify-center">
            <span className="text-red-600 text-xl">⚠</span>
          </div>
          <h2 className="text-xl font-semibold">Authentication Error</h2>
          <p className="text-muted-foreground">{auth.error}</p>
          <button 
            onClick={() => window.location.reload()} 
            className="inline-flex items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  // If user is authenticated, render children
  if (auth.isAuthenticated && auth.user) {
    return <>{children}</>;
  }

  // This shouldn't happen due to useRequireAuth redirect, but just in case
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center space-y-4">
        <p className="text-muted-foreground">Redirecting to login...</p>
      </div>
    </div>
  );
}

/**
 * Higher-order component version of ProtectedRoute
 */
export function withAuth<P extends object>(
  Component: React.ComponentType<P>,
  options: { fallback?: React.ReactNode; requireAuth?: boolean } = {}
) {
  return function AuthenticatedComponent(props: P) {
    return (
      <ProtectedRoute {...options}>
        <Component {...props} />
      </ProtectedRoute>
    );
  };
}

/**
 * Component for pages that should only be shown to unauthenticated users
 * (like login/register pages)
 */
interface GuestOnlyRouteProps {
  children: React.ReactNode;
  redirectTo?: string;
}

export function GuestOnlyRoute({ 
  children, 
  redirectTo = '/dashboard' 
}: GuestOnlyRouteProps) {
  const auth = useAuth();

  // If loading, show children (login/register form)
  if (auth.isLoading) {
    return <>{children}</>;
  }

  // If authenticated, redirect to dashboard or specified route
  if (auth.isAuthenticated) {
    window.location.href = redirectTo;
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center space-y-4">
          <Loader2 className="h-8 w-8 animate-spin mx-auto text-primary" />
          <p className="text-muted-foreground">Redirecting...</p>
        </div>
      </div>
    );
  }

  // If not authenticated, show the guest content
  return <>{children}</>;
}