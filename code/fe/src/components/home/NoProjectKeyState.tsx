import { Key, ArrowRight } from "lucide-react";
import { Button } from "../ui/button";
import { useAuth, hasRole } from "@/contexts/AuthContext";
import { AccountRoles } from "@/generated/api";

export function NoProjectKeyState() {
  const { user } = useAuth();
  const isAdmin = hasRole(user, AccountRoles.AdminUserRole);

  return (
    <div className="flex flex-col items-center justify-center py-16 px-4">
      <div className="flex h-20 w-20 items-center justify-center rounded-full bg-primary/10 mb-6">
        <Key className="h-10 w-10 text-primary" />
      </div>

      <h2 className="text-2xl font-bold mb-3">Welcome to Witnes</h2>
      <p className="text-muted-foreground text-center max-w-md mb-8">
        {isAdmin
          ? "Get started by adding your project key. This unique identifier allows you to track performance metrics for your website."
          : "Please ask your Admin to create a new project key."}
      </p>

      {isAdmin ? (
        <Button
          size="lg"
          onClick={() => window.location.href = "/dashboard/usage"}
        >
          View Project Key
          <ArrowRight className="ml-2 h-4 w-4" />
        </Button>
      ) : null}
    </div>
  );
}
