import { AuthProvider } from '../contexts/AuthContext';
import { Navbar } from './Navbar';

interface NavbarWrapperProps {
  currentPath?: string;
}

export function NavbarWrapper({ currentPath }: NavbarWrapperProps) {
  return (
    <AuthProvider>
      <Navbar currentPath={currentPath} />
    </AuthProvider>
  );
}