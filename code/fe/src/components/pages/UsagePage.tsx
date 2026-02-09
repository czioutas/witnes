import { useEffect, useState } from "react";
import { getWitnesServerAPI, type UsageStatsModel, type PackageInfoModel, type ProjectKeyModel, type CreateProjectKeyRequest } from "../../generated/api";
import { useApiToast } from "../../hooks/useApiToast";
import { ProjectKeyDisplay } from "../usage/ProjectKeyDisplay";
import { UsageStats } from "../usage/UsageStats";
import { PackageInfo } from "../usage/PackageInfo";
import { Button } from "../ui/button";
import { RefreshCw } from "lucide-react";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "../ui/form";
import { Input } from "../ui/input";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";

const api = getWitnesServerAPI();

const createKeySchema = z.object({
  name: z.string().min(1, "Name is required").max(200, "Name must be 200 characters or less"),
  domain: z.string().min(1, "Domain is required"),
});

type CreateKeyFormValues = z.infer<typeof createKeySchema>;

export function UsagePage() {
  const { handleApiCall } = useApiToast();
  const [usageStats, setUsageStats] = useState<UsageStatsModel | null>(null);
  const [packageInfo, setPackageInfo] = useState<PackageInfoModel | null>(null);
  const [projectKeys, setProjectKeys] = useState<ProjectKeyModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const createKeyForm = useForm<CreateKeyFormValues>({
    resolver: zodResolver(createKeySchema),
    defaultValues: {
      name: "",
      domain: "",
    },
  });

  const fetchAllData = async () => {
    setLoading(true);

    // Fetch usage stats
    await handleApiCall({
      apiCall: () => api.getApiV1ProjectKeysUsage(),
      onSuccess: (response) => {
        setUsageStats(response.data);
      },
      showErrorToast: false,
    });

    // Fetch package info
    await handleApiCall({
      apiCall: () => api.getApiV1ProjectKeysPackage(),
      onSuccess: (response) => {
        setPackageInfo(response.data);
      },
      showErrorToast: false,
    });

    // Fetch project keys
    await handleApiCall({
      apiCall: () => api.getApiV1ProjectKeys(),
      onSuccess: (response) => {
        setProjectKeys(response.data);
      },
      showErrorToast: false,
    });

    setLoading(false);
  };

  useEffect(() => {
    fetchAllData();
  }, []);

  const handleRefresh = () => {
    fetchAllData();
  };

  const handleCreateKey = () => {
    createKeyForm.reset();
    setCreateDialogOpen(true);
  };

  const onCreateKeySubmit = async (values: CreateKeyFormValues) => {
    setSubmitting(true);

    const createRequest: CreateProjectKeyRequest = {
      name: values.name,
      domain: values.domain,
    };

    await handleApiCall({
      apiCall: () => api.postApiV1ProjectKeys(createRequest),
      successMessage: "Project key created successfully",
      onSuccess: () => {
        setCreateDialogOpen(false);
        createKeyForm.reset();
        fetchAllData();
        setSubmitting(false);
      },
      onError: () => setSubmitting(false),
    });
  };

  // Get the first active project key (or first key if none active)
  const primaryKey = projectKeys.find(key => key.is_active) || projectKeys[0] || null;

  return (
    <>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Usage</h1>
            <p className="text-muted-foreground">
              Monitor your API usage and manage your subscription plan
            </p>
          </div>
          <Button onClick={handleRefresh} variant="outline" disabled={loading}>
            <RefreshCw className={`mr-2 h-4 w-4 ${loading ? "animate-spin" : ""}`} />
            Refresh
          </Button>
        </div>

        {/* Usage Statistics Cards */}
        <UsageStats stats={usageStats} loading={loading} />

        {/* Package Information */}
        <PackageInfo packageInfo={packageInfo} loading={loading} />

        {/* Project Key Display */}
        <ProjectKeyDisplay
          projectKey={primaryKey}
          onCreateKey={handleCreateKey}
          loading={loading}
        />
      </div>

      {/* Create Project Key Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Project Key</DialogTitle>
            <DialogDescription>
              Generate a new API key for your application
            </DialogDescription>
          </DialogHeader>

          <Form {...createKeyForm}>
            <form
              onSubmit={createKeyForm.handleSubmit(onCreateKeySubmit)}
              className="space-y-4"
            >
              <FormField
                control={createKeyForm.control}
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
                control={createKeyForm.control}
                name="domain"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Domain</FormLabel>
                    <FormControl>
                      <Input placeholder="example.com" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setCreateDialogOpen(false)}
                  disabled={submitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Creating..." : "Create Key"}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </>
  );
}
