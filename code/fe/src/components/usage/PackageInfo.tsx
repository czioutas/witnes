import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../ui/card";
import { Progress } from "../ui/progress";
import { Badge } from "../ui/badge";
import { CalendarDays, DollarSign, Zap } from "lucide-react";
import type { PackageInfoModel } from "../../generated/api";

interface PackageInfoProps {
  packageInfo: PackageInfoModel | null;
  loading: boolean;
}

export function PackageInfo({ packageInfo, loading }: PackageInfoProps) {
  const formatNumber = (num: number | undefined) => {
    if (num === undefined) return "0";
    return num.toLocaleString("en-US");
  };

  const formatCurrency = (amount: number | undefined) => {
    if (amount === undefined) return "$0.00";
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return "Not set";
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const calculateUsagePercentage = () => {
    if (!packageInfo?.page_loads_limit || !packageInfo?.current_month_page_loads) {
      return 0;
    }
    return Math.min(
      (packageInfo.current_month_page_loads / packageInfo.page_loads_limit) * 100,
      100
    );
  };

  const getUsageColor = (percentage: number) => {
    if (percentage >= 90) return "text-red-600";
    if (percentage >= 75) return "text-yellow-600";
    return "text-green-600";
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Package Information</CardTitle>
          <CardDescription>Loading package details...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (!packageInfo) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Package Information</CardTitle>
          <CardDescription>No package information available</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  const usagePercentage = calculateUsagePercentage();
  const remainingLoads = (packageInfo.page_loads_limit || 0) - (packageInfo.current_month_page_loads || 0);

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>Package Information</CardTitle>
            <CardDescription>Your current subscription plan and usage</CardDescription>
          </div>
          <Badge variant={packageInfo.is_active ? "default" : "secondary"}>
            {packageInfo.is_active ? "Active" : "Inactive"}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Plan Details */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">Current Plan</p>
              <p className="text-2xl font-bold">{packageInfo.plan_name || "Unknown"}</p>
              {packageInfo.plan_description && (
                <p className="text-sm text-muted-foreground">
                  {packageInfo.plan_description}
                </p>
              )}
            </div>
            <Zap className="h-8 w-8 text-primary" />
          </div>
        </div>

        {/* Usage Progress */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="font-medium">Monthly Usage</span>
            <span className={getUsageColor(usagePercentage)}>
              {formatNumber(packageInfo.current_month_page_loads)} / {formatNumber(packageInfo.page_loads_limit)}
            </span>
          </div>
          <Progress value={usagePercentage} className="h-2" />
          <p className="text-xs text-muted-foreground">
            {remainingLoads > 0 ? (
              <>
                {formatNumber(remainingLoads)} page loads remaining this month
              </>
            ) : (
              <>
                You have exceeded your monthly limit
              </>
            )}
          </p>
        </div>

        {/* Pricing Details */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2 border-t">
          <div className="flex items-start gap-3">
            <DollarSign className="h-5 w-5 text-muted-foreground mt-0.5" />
            <div>
              <p className="text-sm text-muted-foreground">Monthly Price</p>
              <p className="text-lg font-semibold">
                {formatCurrency(packageInfo.price_per_month)}
              </p>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <DollarSign className="h-5 w-5 text-muted-foreground mt-0.5" />
            <div>
              <p className="text-sm text-muted-foreground">Extra Page Load</p>
              <p className="text-lg font-semibold">
                {formatCurrency(packageInfo.price_per_extra_request)}
              </p>
            </div>
          </div>
        </div>

        {/* Renewal Date */}
        <div className="flex items-center gap-3 pt-2 border-t">
          <CalendarDays className="h-5 w-5 text-muted-foreground" />
          <div>
            <p className="text-sm text-muted-foreground">Next Renewal</p>
            <p className="text-base font-medium">
              {formatDate(packageInfo.renewal_date)}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
