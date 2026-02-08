import { useState } from "react";
import { AuthProvider, useAuth } from "../../contexts/AuthContext";
import { GuestOnlyRoute } from "../ProtectedRoute";
import { Alert, AlertDescription } from "../ui/alert";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Loader2 } from "lucide-react";

function LoginFormContent() {
  const { login, isLoading, error, clearError } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();

    try {
      await login({ email, password });

      // Handle redirect after successful login
      const urlParams = new URLSearchParams(window.location.search);
      const redirectPath = urlParams.get("redirect") || "/dashboard";
      window.location.href = redirectPath;
    } catch (err) {
      // Error is handled by the auth context
    }
  };

  const handleDemoLogin = async () => {
    const email = "mamaslittlebakery@witnes.io";
    const password = "witnesDemoAa1!";

    clearError();
    try {
      await login({ email, password });

      const urlParams = new URLSearchParams(window.location.search);
      const redirectPath = urlParams.get("redirect") || "/dashboard";
      window.location.href = redirectPath;
    } catch (err) {
      // Error is handled by the auth context
    }
  };

  return (
    <Card className="w-full border-border">
      <CardHeader className="space-y-1">
        <CardTitle className="text-2xl font-bold tracking-tight">
          Welcome back
        </CardTitle>
        <CardDescription>
          Enter your email and password to sign in
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@company.com"
              required
              disabled={isLoading}
            />
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label htmlFor="password">Password</Label>
              <a
                href="/authenticate/forgot-password"
                className="text-xs text-muted-foreground hover:text-foreground transition-colors"
              >
                Forgot password?
              </a>
            </div>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              disabled={isLoading}
            />
          </div>

          <Button type="submit" className="w-full" disabled={isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Signing in...
              </>
            ) : (
              "Sign in"
            )}
          </Button>

          <div className="relative my-4">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-2 text-muted-foreground">
                Or continue with
              </span>
            </div>
          </div>

          <Button
            type="button"
            onClick={handleDemoLogin}
            disabled={isLoading}
            variant="outline"
            className="w-full"
          >
            Sign in as Demo
          </Button>

          <div className="text-center text-sm text-muted-foreground">
            Don't have an account?{" "}
            <a
              href="/authenticate/register"
              className="font-medium text-foreground hover:underline"
            >
              Sign up
            </a>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

export function LoginWrapper() {
  return (
    <AuthProvider>
      <GuestOnlyRoute>
        <LoginFormContent />
      </GuestOnlyRoute>
    </AuthProvider>
  );
}
