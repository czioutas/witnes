import { Mail } from "lucide-react";
import { Button } from "../ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";

export function CheckEmailWrapper() {
  return (
    <div className="w-full space-y-6">
      <Card>
        <CardHeader className="space-y-1">
          <div className="flex justify-center mb-4">
            <div className="rounded-full bg-primary/10 p-3">
              <Mail className="h-8 w-8 text-primary" />
            </div>
          </div>
          <CardTitle className="text-2xl text-center">
            Check your email
          </CardTitle>
          <CardDescription className="text-center">
            We've sent a verification link to your email address. Please check
            your inbox and click the link to verify your account.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground text-center">
              Didn't receive the email? Check your spam folder or try
              registering again.
            </p>
            <Button asChild variant="outline" className="w-full">
              <a href="/authenticate/login">Go to login</a>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
