import { useEffect, useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { getWitnesServerAPI, type PublicFileModel } from "../../generated/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "../ui/form";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { MapPin } from "lucide-react";
import { toast } from "sonner";
import { useApiToast } from "../../hooks/useApiToast";

const api = getWitnesServerAPI();

const organizationSettingsSchema = z.object({
  identifier: z.string().min(1, "Organization name is required").max(255),
  vat_number: z.string().max(50).optional(),
  company_registration_number: z.string().max(50).optional(),
  number_of_employees: z.number().int().positive().optional().nullable(),
  street_line_1: z.string().max(255).optional(),
  street_line_2: z.string().max(255).optional(),
  city: z.string().max(100).optional(),
  state_province: z.string().max(100).optional(),
  postal_code: z.string().max(20).optional(),
  country: z.string().max(100).optional(),
});

type OrganizationSettingsFormValues = z.infer<
  typeof organizationSettingsSchema
>;

export function OrganizationSettingsPage() {
  const { user, isAuthenticated } = useAuth();
  const { handleApiCall } = useApiToast();
  const [submitting, setSubmitting] = useState(false);
  const [, setLoading] = useState(true);
  const [normalizedIdentifier, setNormalizedIdentifier] = useState("");
  const [logo, setLogo] = useState<PublicFileModel | null>(null);

  const form = useForm<OrganizationSettingsFormValues>({
    resolver: zodResolver(organizationSettingsSchema),
    defaultValues: {
      identifier: "",
      vat_number: "",
      company_registration_number: "",
      number_of_employees: null,
      street_line_1: "",
      street_line_2: "",
      city: "",
      state_province: "",
      postal_code: "",
      country: "",
    },
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      // Fetch tenant info with all details
      const tenantResponse = await api.getV1Tenant();
      const tenant = tenantResponse.data;

      // Store normalized identifier
      setNormalizedIdentifier(tenant.normalized_identifier);

      // Store logo if present
      if (tenant.logo) {
        setLogo(tenant.logo);
      }

      // Populate all form fields from API response
      form.setValue("identifier", tenant.identifier);
      form.setValue("vat_number", tenant.vat_number || "");
      form.setValue(
        "company_registration_number",
        tenant.company_registration_number || "",
      );

      form.setValue("number_of_employees", tenant.number_of_employees);
      form.setValue("street_line_1", tenant.street_line1 || "");
      form.setValue("street_line_2", tenant.street_line2 || "");
      form.setValue("city", tenant.city || "");
      form.setValue("state_province", tenant.state_province || "");
      form.setValue("postal_code", tenant.postal_code || "");
      form.setValue("country", tenant.country || "");
    } catch (error) {
      console.error("Error fetching organization data:", error);
      toast.error("Failed to load organization details");
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (values: OrganizationSettingsFormValues) => {
    setSubmitting(true);

    await handleApiCall({
      apiCall: () =>
        api.putV1Tenant({
          identifier: values.identifier,
          vat_number: values.vat_number || null,
          company_registration_number:
            values.company_registration_number || null,
          number_of_employees: values.number_of_employees,
          street_line1: values.street_line_1 || null,
          street_line2: values.street_line_2 || null,
          city: values.city || null,
          state_province: values.state_province || null,
          postal_code: values.postal_code || null,
          country: values.country || null,
        }),
      successMessage: "Organization details updated successfully",
      onSuccess: () => setSubmitting(false),
      onError: () => setSubmitting(false),
    });
  };

  if (!isAuthenticated || !user) {
    return (
      <div className="flex items-center justify-center h-[400px]">
        <p className="text-muted-foreground">Not authenticated</p>
      </div>
    );
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        {/* Organization Details Card */}
        <Card>
          <CardHeader>
            <div className="flex items-start gap-6">
              {/* Header text */}
              <div className="flex-1">
                <CardTitle>Organization Details</CardTitle>
                <CardDescription>
                  Manage your organization's information and settings
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="identifier"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Organization Name *</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. Acme Corp" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="space-y-2">
                <FormLabel>Normalized Identifier</FormLabel>
                <input
                  type="text"
                  value={normalizedIdentifier}
                  disabled
                  className="flex h-10 w-full rounded-md border border-input bg-muted px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  readOnly
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="vat_number"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>VAT Number</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. GB123456789" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="company_registration_number"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Company Registration Number</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. 12345678" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
          </CardContent>
        </Card>

        {/* Business Address Card */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-4">
              <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                <MapPin className="h-6 w-6 text-primary" />
              </div>
              <div className="flex-1">
                <CardTitle>Business Address</CardTitle>
                <CardDescription>
                  Your organization's physical location
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <FormField
              control={form.control}
              name="street_line_1"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Street Address Line 1</FormLabel>
                  <FormControl>
                    <Input placeholder="i.e. 123 Business Street" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="street_line_2"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Street Address Line 2</FormLabel>
                  <FormControl>
                    <Input placeholder="i.e. Suite 100" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="city"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>City</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. London" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="state_province"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>State/Province</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. England" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="postal_code"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Postal Code</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. SW1A 1AA" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="country"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Country</FormLabel>
                    <FormControl>
                      <Input placeholder="i.e. United Kingdom" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
          </CardContent>
        </Card>

        {/* Save Button */}
        <div className="flex justify-end">
          <Button type="submit" disabled={submitting}>
            {submitting ? "Saving..." : "Save Changes"}
          </Button>
        </div>
      </form>
    </Form>
  );
}
