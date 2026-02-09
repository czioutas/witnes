"use client";

import { useEffect, useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import { getWitnesServerAPI, type TenantCustomerSummaryModel } from "../../generated/api";
import { UserInfoHeader } from "../customers/UserInfoHeader";
import { UserPageLoadsTable } from "../customers/UserPageLoadsTable";
import { Button } from "../ui/button";
import { ArrowLeft } from "lucide-react";

interface UserDetailPageProps {
  userId: string;
}

export default function UserDetailPage({ userId }: UserDetailPageProps) {
  const { handleApiCall } = useApiToast();
  const [userSummary, setUserSummary] = useState<TenantCustomerSummaryModel | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchUserSummary = async () => {
      setLoading(true);
      const api = getWitnesServerAPI();
      await handleApiCall({
        apiCall: async () => {
          const response = await api.getApiV1TenantCustomers({
            UserIdSearch: userId,
            PageNumber: 1,
            PageSize: 1,
          });
          return response.data;
        },
        onSuccess: (data) => {
          if (data.data && data.data.length > 0) {
            setUserSummary(data.data[0]);
          }
          setLoading(false);
        },
        onError: () => setLoading(false),
      });
    };

    fetchUserSummary();
  }, [userId]);

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => window.history.back()}
        >
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back
        </Button>
      </div>

      {loading ? (
        <div className="text-center py-8 text-muted-foreground">
          Loading user details...
        </div>
      ) : (
        <>
          <UserInfoHeader
            userId={userId}
            totalPageLoads={userSummary?.total_page_loads}
            browsers={userSummary?.browsers}
            operatingSystems={userSummary?.operating_systems}
          />

          <div>
            <h2 className="text-xl font-semibold mb-4">Page Loads</h2>
            <UserPageLoadsTable userId={userId} />
          </div>
        </>
      )}
    </div>
  );
}
