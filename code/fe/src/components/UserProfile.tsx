import { Avatar, AvatarFallback } from './ui/avatar';
import { useAuth } from '../contexts/AuthContext';

interface UserProfileProps {
  showEmail?: boolean;
  avatarSize?: 'sm' | 'md' | 'lg';
  layout?: 'horizontal' | 'vertical';
}

export function UserProfile({ 
  showEmail = true, 
  avatarSize = 'md',
  layout = 'horizontal' 
}: UserProfileProps) {
  const { user } = useAuth();

  if (!user) {
    return null;
  }

  const displayName = `${user.firstName} ${user.lastName}`;
  const initials = `${user.firstName?.charAt(0) || ''}${user.lastName?.charAt(0) || ''}`.toUpperCase();
  
  const avatarSizeClasses = {
    sm: 'h-6 w-6',
    md: 'h-8 w-8',
    lg: 'h-10 w-10'
  };

  const textSizeClasses = {
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-base'
  };

  if (layout === 'vertical') {
    return (
      <div className="flex flex-col items-center space-y-2">
        <Avatar className={avatarSizeClasses[avatarSize]}>
          <AvatarFallback>{initials}</AvatarFallback>
        </Avatar>
        <div className="text-center space-y-1">
          <p className={`font-medium leading-none ${textSizeClasses[avatarSize]}`}>
            {displayName}
          </p>
          {showEmail && (
            <p className={`leading-none text-muted-foreground ${textSizeClasses[avatarSize === 'lg' ? 'md' : 'sm']}`}>
              {user.email}
            </p>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center space-x-3">
      <Avatar className={avatarSizeClasses[avatarSize]}>
        <AvatarFallback>{initials}</AvatarFallback>
      </Avatar>
      <div className="flex-1 space-y-1">
        <p className={`font-medium leading-none ${textSizeClasses[avatarSize]}`}>
          {displayName}
        </p>
        {showEmail && (
          <p className={`leading-none text-muted-foreground ${textSizeClasses[avatarSize === 'lg' ? 'md' : 'sm']}`}>
            {user.email}
          </p>
        )}
      </div>
    </div>
  );
}