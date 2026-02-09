import { useState, useEffect } from "react";
import { getWitnesServerAPI, type TenantCustomerSummaryModel } from "@/generated/api";
import { NoProjectKeyState } from "../home/NoProjectKeyState";
import { NoDataState } from "../home/NoDataState";
import { ActiveState } from "../home/ActiveState";

type HomeState = "loading" | "no-key" | "no-data" | "active";

export function Home() {
  const [state, setState] = useState<HomeState>("loading");
  const [projectKey, setProjectKey] = useState<string | null>(null);
  const [recentUsers, setRecentUsers] = useState<TenantCustomerSummaryModel[]>([]);
  const [totalUsers, setTotalUsers] = useState<number>(0);
  const [dataIngestionWorking, setDataIngestionWorking] = useState(false);

  useEffect(() => {
    checkHomeState();
  }, []);

  const checkHomeState = async () => {
    const api = getWitnesServerAPI();

    try {
      // Step 1: Check if project key exists
      const keysResponse = await api.getApiV1ProjectKeys();
      const keys = keysResponse.data;

      if (!keys || keys.length === 0) {
        setState("no-key");
        return;
      }

      const key = keys[0];
      setProjectKey(key.project_key || null);

      // Step 2: Check if there's data in the past 24 hours
      const oneDayAgo = new Date();
      oneDayAgo.setDate(oneDayAgo.getDate() - 1);

      const customersResponse = await api.getApiV1TenantCustomers({
        StartDate: oneDayAgo.toISOString(),
        PageNumber: 1,
        PageSize: 4,
      });

      const customersData = customersResponse.data;

      if (!customersData.data || customersData.data.length === 0) {
        setState("no-data");
        return;
      }

      // Step 3: Active state - fetch recent users and total count
      setRecentUsers(customersData.data.slice(0, 4));
      setTotalUsers(customersData.total_count || 0);
      setDataIngestionWorking(true);
      setState("active");
    } catch (error) {
      console.error("Error checking home state:", error);
      // Default to no-key state on error
      setState("no-key");
    }
  };

  if (state === "loading") {
    return (
      <div className="mx-auto w-full max-w-6xl">
        <div className="flex items-center justify-center py-16">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
            <p className="text-muted-foreground">Loading...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-6xl">
      {state === "no-key" && <NoProjectKeyState />}
      {state === "no-data" && (
        <NoDataState projectKey={projectKey || undefined} />
      )}
      {state === "active" && (
        <ActiveState
          recentUsers={recentUsers}
          totalUsers={totalUsers}
          dataIngestionWorking={dataIngestionWorking}
        />
      )}
    </div>
  );
}
