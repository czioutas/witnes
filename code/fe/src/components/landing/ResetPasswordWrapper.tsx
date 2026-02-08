import { useState, useEffect } from "react";
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
import { Alert, AlertDescription } from "../ui/alert";
import { CheckCircle2, Loader2 } from "lucide-react";
import { getWitnesServerAPI } from "../../generated/api";

const api = getWitnesServerAPI();

export function ResetPasswordWrapper() {
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [code, setCode] = useState("");
  const [userId, setUserId] = useState("");

  useEffect(() => {
    // Read code and userId from URL query parameters
    const params = new URLSearchParams(window.location.search);
    const codeParam = params.get("code");
    const userIdParam = params.get("userId");

    if (!codeParam || !userIdParam) {
      setError("Invalid or missing reset link parameters");
    } else {
      setCode(codeParam);
      setUserId(userIdParam);
    }
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (newPassword !== confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    if (newPassword.length < 8) {
      setError("Password must be at least 8 characters");
      return;
    }

    if (!code || !userId) {
      setError("Invalid reset link");
      return;
    }

    setIsLoading(true);

    try {
      await api.postV1ResetPassword({
        code,
        new_password: newPassword,
        user_id: userId,
      });
      setSuccess(true);
    } catch (err: any) {
      setError("Failed to reset password. The reset link may have expired.");
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <Card className="w-full border-border">
        <CardContent className="pt-6">
          <div className="flex flex-col items-center text-center space-y-4">
            <div className="rounded-full bg-primary/10 p-4">
              <CheckCircle2 className="h-12 w-12 text-primary" />
            </div>
            <div className="space-y-2">
              <h2 className="text-2xl font-bold tracking-tight text-foreground">
                Password reset successful
              </h2>
              <p className="text-sm text-muted-foreground">
                Your password has been updated. You can now log in with your new
                password.
              </p>
            </div>
            <Button asChild className="w-full">
              <a href="/authenticate/login">Go to login</a>
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="w-full border-border">
      <CardHeader className="space-y-1">
        <CardTitle className="text-2xl font-bold tracking-tight">
          Set new password
        </CardTitle>
        <CardDescription>Enter your new password below</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label htmlFor="newPassword">New Password</Label>
            <Input
              id="newPassword"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Enter new password"
              required
              disabled={isLoading}
              minLength={8}
            />
            <p className="text-xs text-muted-foreground">
              Must be at least 8 characters
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm Password</Label>
            <Input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="Confirm new password"
              required
              disabled={isLoading}
              minLength={8}
            />
          </div>

          <Button type="submit" className="w-full" disabled={isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Resetting password...
              </>
            ) : (
              "Reset password"
            )}
          </Button>

          <div className="text-center text-sm text-muted-foreground">
            Remember your password?{" "}
            <a
              href="/authenticate/login"
              className="font-medium text-foreground hover:underline"
            >
              Sign in
            </a>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
