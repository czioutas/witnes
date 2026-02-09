import { useState } from "react";
import { Button } from "../ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../ui/card";
import { Copy, Check, Plus, AlertCircle } from "lucide-react";
import type { ProjectKeyModel } from "../../generated/api";
import { Alert, AlertDescription } from "../ui/alert";

interface ProjectKeyDisplayProps {
  projectKey: ProjectKeyModel | null;
  onCreateKey: () => void;
  loading: boolean;
}

export function ProjectKeyDisplay({ projectKey, onCreateKey, loading }: ProjectKeyDisplayProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    if (!projectKey?.project_key) return;

    try {
      await navigator.clipboard.writeText(projectKey.project_key);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error("Failed to copy:", err);
    }
  };

  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return "Never used";
    return new Date(dateString).toLocaleString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Project Key</CardTitle>
          <CardDescription>Loading project key information...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (!projectKey) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Project Key</CardTitle>
          <CardDescription>No project key found for your organization</CardDescription>
        </CardHeader>
        <CardContent>
          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              You need to create a project key to start tracking page loads. Click the button below to generate your first key.
            </AlertDescription>
          </Alert>
          <Button onClick={onCreateKey} className="mt-4">
            <Plus className="mr-2 h-4 w-4" />
            Create Project Key
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Project Key</CardTitle>
        <CardDescription>
          Use this key to integrate Witnes tracking into your application
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">API Key</label>
          <div className="flex gap-2">
            <div className="flex-1 rounded-md border bg-muted px-3 py-2 font-mono text-sm">
              {projectKey.project_key || "No key available"}
            </div>
            <Button
              variant="outline"
              size="icon"
              onClick={handleCopy}
              disabled={!projectKey.project_key}
            >
              {copied ? (
                <Check className="h-4 w-4 text-green-600" />
              ) : (
                <Copy className="h-4 w-4" />
              )}
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
          <div>
            <p className="text-sm text-muted-foreground">Status</p>
            <p className="text-base font-medium">
              {projectKey.is_active ? (
                <span className="text-green-600">Active</span>
              ) : (
                <span className="text-red-600">Inactive</span>
              )}
            </p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">Last Used</p>
            <p className="text-base font-medium">
              {formatDate(projectKey.last_used_at)}
            </p>
          </div>
        </div>

        {projectKey.description && (
          <div className="pt-2">
            <p className="text-sm text-muted-foreground">Description</p>
            <p className="text-base">{projectKey.description}</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
