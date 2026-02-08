import { AuthProvider } from "../../contexts/AuthContext";
import NavbarClient from "./NavbarClient";

export default function NavbarWithAuth() {
  return (
    <AuthProvider>
      <NavbarClient />
    </AuthProvider>
  );
}
