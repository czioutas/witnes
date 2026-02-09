import { useState } from "react";
import { Search, TrendingUp, Users, Activity } from "lucide-react";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import type { TenantCustomerSummaryModel } from "../../generated/api";

interface ActiveStateProps {
  recentUsers: TenantCustomerSummaryModel[];
  totalUsers?: number;
  dataIngestionWorking: boolean;
}

export function ActiveState({
  recentUsers,
  totalUsers,
  dataIngestionWorking,
}: ActiveStateProps) {
  const [searchQuery, setSearchQuery] = useState("");

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      window.location.href = `/dashboard/customers?search=${encodeURIComponent(searchQuery)}`;
    }
  };

  return (
    <div className="space-y-8">
      {/* Data Ingestion Status */}
      {dataIngestionWorking && (
        <div className="flex items-center gap-3 p-4 bg-green-50 dark:bg-green-950/20 border border-green-200 dark:border-green-900 rounded-lg">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-green-500/10">
            <TrendingUp className="h-5 w-5 text-green-600 dark:text-green-400" />
          </div>
          <div>
            <h3 className="font-semibold text-green-900 dark:text-green-100">
              Data Ingestion Active
            </h3>
            <p className="text-sm text-green-700 dark:text-green-300">
              Your website is being monitored and performance data is flowing in.
            </p>
          </div>
        </div>
      )}

      {/* Large Search Component */}
      <div className="text-center space-y-6">
        <div>
          <h1 className="text-4xl font-bold mb-3">Search Users</h1>
          <p className="text-muted-foreground text-lg">
            Find any monitored user by their ID to view their activity and page loads
          </p>
        </div>

        <form onSubmit={handleSearch} className="max-w-2xl mx-auto">
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 h-5 w-5 text-muted-foreground" />
              <Input
                type="text"
                placeholder="Enter user ID..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-12 h-14 text-lg"
              />
            </div>
            <Button type="submit" size="lg" className="h-14 px-8">
              Search
            </Button>
          </div>
        </form>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="p-6 border rounded-lg">
          <div className="flex items-center gap-3 mb-2">
            <Users className="h-5 w-5 text-primary" />
            <h3 className="font-semibold">Total Users Monitored</h3>
          </div>
          <p className="text-3xl font-bold">{totalUsers?.toLocaleString() || "0"}</p>
        </div>

        <div className="p-6 border rounded-lg">
          <div className="flex items-center gap-3 mb-2">
            <Activity className="h-5 w-5 text-primary" />
            <h3 className="font-semibold">Recent Activity</h3>
          </div>
          <p className="text-3xl font-bold">{recentUsers.length}</p>
          <p className="text-sm text-muted-foreground">users in the last 24 hours</p>
        </div>
      </div>

      {/* Recent Users */}
      {recentUsers.length > 0 && (
        <div>
          <h2 className="text-2xl font-bold mb-4">Recent Users</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {recentUsers.map((user) => (
              <div
                key={user.user_id}
                className="p-4 border rounded-lg hover:bg-muted/50 cursor-pointer transition-colors"
                onClick={() =>
                  (window.location.href = `/dashboard/customers/${encodeURIComponent(user.user_id!)}`)
                }
              >
                <div className="flex items-center gap-2 mb-2">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10">
                    <Users className="h-4 w-4 text-primary" />
                  </div>
                  <p className="font-medium truncate">{user.user_id}</p>
                </div>
                <div className="space-y-1 text-sm text-muted-foreground">
                  <p>{user.total_page_loads} page loads</p>
                  <p>
                    Last seen:{" "}
                    {user.last_seen_at
                      ? new Date(user.last_seen_at).toLocaleDateString()
                      : "-"}
                  </p>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-6 text-center">
            <Button
              variant="outline"
              onClick={() => (window.location.href = "/dashboard/customers")}
            >
              View All Users
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
