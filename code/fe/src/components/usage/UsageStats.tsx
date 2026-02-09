import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../ui/card";
import { Activity, TrendingUp, Clock } from "lucide-react";
import type { UsageStatsModel } from "../../generated/api";

interface UsageStatsProps {
  stats: UsageStatsModel | null;
  loading: boolean;
}

export function UsageStats({ stats, loading }: UsageStatsProps) {
  const formatNumber = (num: number | undefined) => {
    if (num === undefined) return "0";
    return num.toLocaleString("en-US");
  };

  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return "No activity yet";
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
      <div className="grid gap-4 md:grid-cols-3">
        {[1, 2, 3].map((i) => (
          <Card key={i}>
            <CardHeader className="space-y-2">
              <CardDescription>Loading...</CardDescription>
            </CardHeader>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <div className="grid gap-4 md:grid-cols-3">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">
            Total Page Loads
          </CardTitle>
          <Activity className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">
            {formatNumber(stats?.total_page_loads)}
          </div>
          <p className="text-xs text-muted-foreground mt-1">
            All-time usage
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">
            Current Month
          </CardTitle>
          <TrendingUp className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">
            {formatNumber(stats?.current_month_page_loads)}
          </div>
          <p className="text-xs text-muted-foreground mt-1">
            Page loads this month
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">
            Last Activity
          </CardTitle>
          <Clock className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="text-sm font-medium">
            {formatDate(stats?.last_page_load_at)}
          </div>
          <p className="text-xs text-muted-foreground mt-1">
            Last page load recorded
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
