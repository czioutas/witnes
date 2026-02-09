import { useState } from "react";
import { Button } from "../ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../ui/card";
import { Copy, Check, Plus, AlertCircle, Edit, Trash2, Globe } from "lucide-react";
import { getWitnesServerAPI, type ProjectKeyModel, type UpdateProjectKeyRequest } from "../../generated/api";
import { Alert, AlertDescription } from "../ui/alert";
import { Badge } from "../ui/badge";
import { useApiToast } from "../../hooks/useApiToast";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "../ui/form";
import { Input } from "../ui/input";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";

const api = getWitnesServerAPI();

const updateKeySchema = z.object({
  name: z.string().min(1, "Name is required").max(200, "Name must be 200 characters or less"),
  domain: z.string().min(1, "Domain is required"),
});

type UpdateKeyFormValues = z.infer<typeof updateKeySchema>;

interface ProjectKeyDisplayProps {
  projectKeys: ProjectKeyModel[];
  onCreateKey: () => void;
  onUpdateKey: () => void;
  loading: boolean;
}

export function ProjectKeyDisplay({ projectKeys, onCreateKey, onUpdateKey, loading }: ProjectKeyDisplayProps) {
  const { handleApiCall } = useApiToast();
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [editingKey, setEditingKey] = useState<ProjectKeyModel | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const updateKeyForm = useForm<UpdateKeyFormValues>({
    resolver: zodResolver(updateKeySchema),
    defaultValues: {
      name: "",
      domain: "",
    },
  });

  const handleCopy = async (id: string, key: string) => {
    try {
      await navigator.clipboard.writeText(key);
      setCopiedId(id);
      setTimeout(() => setCopiedId(null), 2000);
    } catch (err) {
      console.error("Failed to copy:", err);
    }
  };

  const handleEdit = (key: ProjectKeyModel) => {
    setEditingKey(key);
    updateKeyForm.reset({
      name: key.name || "",
      domain: key.domain || "",
    });
  };

  const onUpdateKeySubmit = async (values: UpdateKeyFormValues) => {
    if (!editingKey || !editingKey.id) return;
    setSubmitting(true);

    const updateRequest: UpdateProjectKeyRequest = {
      name: values.name,
      domain: values.domain,
    };

    await handleApiCall({
      apiCall: () => api.putApiV1ProjectKeysId(editingKey.id!, updateRequest),
      successMessage: "Project key updated successfully",
      onSuccess: () => {
        setEditingKey(null);
        onUpdateKey();
        setSubmitting(false);
      },
      onError: () => setSubmitting(false),
    });
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Are you sure you want to delete this project key? This action cannot be undone.")) {
      return;
    }

    await handleApiCall({
      apiCall: () => api.deleteApiV1ProjectKeysId(id),
      successMessage: "Project key deleted successfully",
      onSuccess: () => {
        onUpdateKey();
      },
    });
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Project Keys</CardTitle>
          <CardDescription>Loading project key information...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (projectKeys.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Project Keys</CardTitle>
          <CardDescription>No project keys found for your organization</CardDescription>
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
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Project Keys</h2>
        <Button onClick={onCreateKey} size="sm">
          <Plus className="mr-2 h-4 w-4" />
          New Key
        </Button>
      </div>

      <div className="grid gap-4">
        {projectKeys.map((key) => (
          <Card key={key.id}>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <div className="space-y-1">
                  <CardTitle className="text-lg">{key.name}</CardTitle>
                  <CardDescription className="flex items-center gap-2">
                    {key.is_active ? (
                      <Badge variant="outline" className="text-green-600 border-green-200 bg-green-50">Active</Badge>
                    ) : (
                      <Badge variant="outline" className="text-red-600 border-red-200 bg-red-50">Inactive</Badge>
                    )}
                  </CardDescription>
                </div>
                <div className="flex gap-2">
                  <Button variant="outline" size="icon" onClick={() => handleEdit(key)}>
                    <Edit className="h-4 w-4" />
                  </Button>
                  {key.id && (
                    <Button variant="outline" size="icon" className="text-red-600" onClick={() => handleDelete(key.id!)}>
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">API Key</label>
                <div className="flex gap-2">
                  <div className="flex-1 rounded-md border bg-muted px-3 py-2 font-mono text-sm overflow-hidden text-ellipsis whitespace-nowrap">
                    {key.project_key}
                  </div>
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => key.id && key.project_key && handleCopy(key.id, key.project_key)}
                    disabled={!key.project_key}
                  >
                    {copiedId === key.id ? (
                      <Check className="h-4 w-4 text-green-600" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              </div>

              {key.domain && (
                <div className="space-y-2">
                  <label className="text-sm font-medium flex items-center gap-1.5">
                    <Globe className="h-3.5 w-3.5" />
                    Domain
                  </label>
                  <Badge variant="secondary" className="font-mono text-[10px]">
                    {key.domain}
                  </Badge>
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Edit Project Key Dialog */}
      <Dialog open={!!editingKey} onOpenChange={(open) => !open && setEditingKey(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Project Key</DialogTitle>
            <DialogDescription>
              Update your project key settings
            </DialogDescription>
          </DialogHeader>

          <Form {...updateKeyForm}>
            <form
              onSubmit={updateKeyForm.handleSubmit(onUpdateKeySubmit)}
              className="space-y-4"
            >
              <FormField
                control={updateKeyForm.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Key Name</FormLabel>
                    <FormControl>
                      <Input placeholder="Production App" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={updateKeyForm.control}
                name="domain"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Domain</FormLabel>
                    <FormControl>
                      <Input placeholder="example.com" {...field} />
                    </FormControl>
                    <p className="text-xs text-muted-foreground">
                      The domain this key is associated with (e.g. example.com)
                    </p>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setEditingKey(null)}
                  disabled={submitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Updating..." : "Update Key"}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
