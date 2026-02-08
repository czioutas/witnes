import { AuthProvider, useAuth } from '../../contexts/AuthContext';
import { useState, useEffect } from 'react';
import { Settings, LogOut, LayoutDashboard } from 'lucide-react';

function MobileLandingAuthActionsInner() {
  const { user, isAuthenticated, logout } = useAuth();
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const handleLogout = async () => {
    await logout();
    window.location.href = '/';
  };

  if (!isClient || !isAuthenticated) {
    return (
      <a
        href="/authenticate/login"
        className="border border-white px-8 py-4 text-sm uppercase tracking-[0.3em] font-bold text-white hover:bg-white hover:text-black transition-all text-center"
      >
        LOGIN
      </a>
    );
  }

  return (
    <div className="flex flex-col items-center gap-6 w-full">
      {/* User info */}
      <div className="text-center">
        <p className="text-lg font-black text-white mb-1">
          {user?.firstName} {user?.lastName}
        </p>
        {user?.email && (
          <p className="text-xs text-zinc-400">
            {user.email}
          </p>
        )}
      </div>

      {/* Mobile menu actions */}
      <div className="flex flex-col items-center gap-4 w-full max-w-xs">
        <a
          href="/dashboard"
          className="flex items-center justify-center gap-3 w-full px-6 py-4 bg-[#A68B6A] text-white text-xs font-black uppercase tracking-[0.3em] hover:bg-[#8B6E52] transition-colors"
        >
          <LayoutDashboard className="h-4 w-4" />
          <span>Dashboard</span>
        </a>

        <a
          href="/dashboard/settings"
          className="flex items-center justify-center gap-3 w-full px-6 py-3 border border-white/20 text-white text-xs font-bold uppercase tracking-[0.3em] hover:bg-white/10 transition-colors"
        >
          <Settings className="h-4 w-4" />
          <span>Settings</span>
        </a>

        <button
          onClick={handleLogout}
          className="flex items-center justify-center gap-3 w-full px-6 py-3 border border-white/20 text-white text-xs font-bold uppercase tracking-[0.3em] hover:bg-white/10 transition-colors"
        >
          <LogOut className="h-4 w-4" />
          <span>Log out</span>
        </button>
      </div>
    </div>
  );
}

export function MobileLandingAuthActions() {
  return (
    <AuthProvider>
      <MobileLandingAuthActionsInner />
    </AuthProvider>
  );
}
