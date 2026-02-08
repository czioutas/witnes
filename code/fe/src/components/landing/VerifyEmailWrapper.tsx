import { useState, useEffect } from "react";
import { Button } from "../ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Alert, AlertDescription } from "../ui/alert";
import { Loader2, CheckCircle2, XCircle } from "lucide-react";
import { getWitnesServerAPI } from "../../generated/api";

const api = getWitnesServerAPI();

export function VerifyEmailWrapper() {
  const [isLoading, setIsLoading] = useState(true);
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
      setError("Invalid or missing verification link parameters");
      setIsLoading(false);
    } else {
      setCode(codeParam);
      setUserId(userIdParam);
      verifyEmail(codeParam, userIdParam);
    }
  }, []);

  const verifyEmail = async (code: string, userId: string) => {
    setIsLoading(true);
    setError("");

    try {
      await api.postV1VerifyEmail({
        code,
        user_id: userId,
      });
      setSuccess(true);
    } catch (err: any) {
      setError(
        "Failed to verify email. The verification link may have expired.",
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleRetry = () => {
    if (code && userId) {
      verifyEmail(code, userId);
    }
  };

  if (isLoading) {
    return (
      <div className="w-full space-y-6">
        <Card>
          <CardHeader className="space-y-1">
            <div className="flex justify-center mb-4">
              <Loader2 className="h-12 w-12 animate-spin text-primary" />
            </div>
            <CardTitle className="text-2xl text-center">
              Verifying your email
            </CardTitle>
            <CardDescription className="text-center">
              Please wait while we verify your email address...
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  if (success) {
    return (
      <div className="w-full space-y-6">
        <Card>
          <CardHeader className="space-y-1">
            <div className="flex justify-center mb-4">
              <div className="rounded-full bg-green-100 p-3">
                <CheckCircle2 className="h-8 w-8 text-green-600" />
              </div>
            </div>
            <CardTitle className="text-2xl text-center">
              Email verified successfully
            </CardTitle>
            <CardDescription className="text-center">
              Your email has been verified. You can now log in to your account.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <Button asChild className="w-full">
                <a href="/authenticate/login">Go to login</a>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="w-full space-y-6">
      <Card>
        <CardHeader className="space-y-1">
          <div className="flex justify-center mb-4">
            <div className="rounded-full bg-red-100 p-3">
              <XCircle className="h-8 w-8 text-red-600" />
            </div>
          </div>
          <CardTitle className="text-2xl text-center">
            Verification failed
          </CardTitle>
          <CardDescription className="text-center">
            We couldn't verify your email address
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <Button onClick={handleRetry} className="w-full">
              Try again
            </Button>

            <div className="text-center text-sm">
              <a
                href="/authenticate/login"
                className="text-primary hover:underline"
              >
                Back to login
              </a>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
