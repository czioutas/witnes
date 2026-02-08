import { useEffect } from 'react';
import { AuthProvider, useAuth } from '../contexts/AuthContext';

function LogoutContent() {
  const { logout } = useAuth();

  useEffect(() => {
    const performLogout = async () => {
      try {
        await logout();
        
        // Redirect to home page after logout
        setTimeout(() => {
          window.location.href = '/';
        }, 1500);
      } catch (error) {
        console.error('Logout error:', error);
        // Still redirect even if there's an error
        setTimeout(() => {
          window.location.href = '/';
        }, 1500);
      }
    };

    performLogout();
  }, [logout]);

  return (
    <div className="flex-1 flex items-center justify-center">
      <div className="text-center space-y-4">
        <h1 className="text-2xl font-semibold">Logging you out...</h1>
        <p className="text-muted-foreground">Please wait while we sign you out.</p>
      </div>
    </div>
  );
}

export function LogoutWrapper() {
  return (
    <AuthProvider>
      <LogoutContent />
    </AuthProvider>
  );
}