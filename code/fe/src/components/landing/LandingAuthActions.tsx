import { Avatar, AvatarFallback } from '../ui/avatar';
import { LogOut, Settings } from 'lucide-react';
import { AuthProvider, useAuth } from '../../contexts/AuthContext';
import { useState, useEffect } from 'react';
import { Button } from '../ui/button';

function LandingAuthActionsInner() {
  const { user, isAuthenticated, logout } = useAuth();
  const [isClient, setIsClient] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const handleLogout = async () => {
    await logout();
    window.location.href = '/';
  };

  if (!isClient || !isAuthenticated) {
    return (
      <div className="hidden lg:block">
        <Button
          variant="outline"
          size="sm"
          asChild
        >
          <a href="/authenticate/login" className="uppercase tracking-[0.2em] font-bold">
            LOGIN
          </a>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-4 md:gap-6">
      {/* Desktop: Show DASHBOARD link, Mobile: Hide it (shown in mobile menu instead) */}
      <a
        href="/dashboard"
        className="hidden md:block text-sm uppercase tracking-[0.2em] font-bold hover:opacity-50 transition-opacity"
      >
        DASHBOARD
      </a>

      <div className="relative">
        <button
          onClick={() => setShowDropdown(!showDropdown)}
          className="relative h-8 w-8 rounded-full hover:opacity-70 transition-opacity"
        >
          <Avatar className="h-8 w-8 border-2 border-current">
            <AvatarFallback className="bg-black text-white text-xs font-black">
              {user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : 'U'}
            </AvatarFallback>
          </Avatar>
        </button>

        {showDropdown && (
          <>
            {/* Backdrop to close dropdown */}
            <div
              className="fixed inset-0 z-40"
              onClick={() => setShowDropdown(false)}
            />

            {/* Dropdown menu */}
            <div className="absolute right-0 mt-2 w-56 bg-white border border-black/10 shadow-lg z-50">
              <div className="p-4 border-b border-black/5">
                <p className="font-bold text-sm">
                  {user?.firstName} {user?.lastName}
                </p>
                {user?.email && (
                  <p className="text-xs text-zinc-500 truncate mt-1">
                    {user.email}
                  </p>
                )}
              </div>

              <div className="py-2">
                <a
                  href="/dashboard"
                  className="flex items-center gap-3 px-4 py-2 text-sm hover:bg-black/5 transition-colors md:hidden"
                >
                  <span>Dashboard</span>
                </a>

                <a
                  href="/dashboard/settings"
                  className="flex items-center gap-3 px-4 py-2 text-sm hover:bg-black/5 transition-colors"
                >
                  <Settings className="h-4 w-4" />
                  <span>Settings</span>
                </a>

                <button
                  onClick={handleLogout}
                  className="flex items-center gap-3 px-4 py-2 text-sm hover:bg-black/5 transition-colors w-full text-left"
                >
                  <LogOut className="h-4 w-4" />
                  <span>Log out</span>
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

export function LandingAuthActions() {
  return (
    <AuthProvider>
      <LandingAuthActionsInner />
    </AuthProvider>
  );
}
