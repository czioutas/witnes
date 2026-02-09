"use client";

import { useEffect, useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import { getWitnesServerAPI, type PageLoadDetailModel } from "../../generated/api";
import { Button } from "../ui/button";
import { ArrowLeft, Clock, TrendingUp, Gauge } from "lucide-react";
import { NetworkWaterfall } from "../page-loads/NetworkWaterfall";
import { JankReports } from "../page-loads/JankReports";

interface PageLoadDetailPageProps {
  pageLoadId: string;
}

export default function PageLoadDetailPage({ pageLoadId }: PageLoadDetailPageProps) {
  const { handleApiCall } = useApiToast();
  const [pageLoad, setPageLoad] = useState<PageLoadDetailModel | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPageLoadDetail = async () => {
      setLoading(true);
      const api = getWitnesServerAPI();
      await handleApiCall({
        apiCall: async () => {
          const response = await api.getApiV1PageLoadsId(pageLoadId);
          return response.data;
        },
        onSuccess: (data) => {
          setPageLoad(data);
          setLoading(false);
        },
        onError: () => setLoading(false),
      });
    };

    fetchPageLoadDetail();
  }, [pageLoadId]);

  const getHealthGradeColor = (lcp: number, cls: number) => {
    if (lcp < 2500 && cls < 0.1) return "text-green-600 dark:text-green-400";
    if (lcp < 4000 && cls < 0.25) return "text-yellow-600 dark:text-yellow-400";
    return "text-red-600 dark:text-red-400";
  };

  const getHealthGradeLabel = (lcp: number, cls: number) => {
    if (lcp < 2500 && cls < 0.1) return "Good";
    if (lcp < 4000 && cls < 0.25) return "Needs Improvement";
    return "Poor";
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading page load details...</p>
        </div>
      </div>
    );
  }

  if (!pageLoad) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground">Page load not found</p>
      </div>
    );
  }

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

      {/* Page Info Header */}
      <div className="space-y-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight mb-2">Page Load Details</h1>
          <p className="text-muted-foreground break-all">{pageLoad.url}</p>
        </div>

        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-2">
            <Clock className="h-4 w-4" />
            {pageLoad.timestamp ? new Date(pageLoad.timestamp).toLocaleString() : "-"}
          </div>
          <div>User: {pageLoad.user_id}</div>
          <div>Session: {pageLoad.session_id}</div>
        </div>
      </div>

      {/* Core Web Vitals */}
      <div>
        <h2 className="text-xl font-semibold mb-4">Core Web Vitals</h2>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="p-6 border rounded-lg">
            <div className="flex items-center gap-3 mb-2">
              <TrendingUp className="h-5 w-5 text-primary" />
              <h3 className="font-semibold">LCP</h3>
            </div>
            <p className="text-3xl font-bold">{pageLoad.lcp_ms?.toFixed(0)}ms</p>
            <p className="text-sm text-muted-foreground">Largest Contentful Paint</p>
          </div>

          <div className="p-6 border rounded-lg">
            <div className="flex items-center gap-3 mb-2">
              <Gauge className="h-5 w-5 text-primary" />
              <h3 className="font-semibold">CLS</h3>
            </div>
            <p className="text-3xl font-bold">{pageLoad.cls?.toFixed(3)}</p>
            <p className="text-sm text-muted-foreground">Cumulative Layout Shift</p>
          </div>

          <div className="p-6 border rounded-lg">
            <div className="flex items-center gap-3 mb-2">
              <Clock className="h-5 w-5 text-primary" />
              <h3 className="font-semibold">TTFB</h3>
            </div>
            <p className="text-3xl font-bold">{pageLoad.avg_ttfb_ms}ms</p>
            <p className="text-sm text-muted-foreground">Time to First Byte</p>
          </div>

          <div className="p-6 border rounded-lg">
            <div className="flex items-center gap-3 mb-2">
              <h3 className="font-semibold">Overall Health</h3>
            </div>
            <p
              className={`text-3xl font-bold ${getHealthGradeColor(pageLoad.lcp_ms || 0, pageLoad.cls || 0)}`}
            >
              {getHealthGradeLabel(pageLoad.lcp_ms || 0, pageLoad.cls || 0)}
            </p>
          </div>
        </div>
      </div>

      {/* Jank Reports */}
      <div>
        <h2 className="text-xl font-semibold mb-4">Performance Issues</h2>
        <JankReports jankReports={pageLoad.jank_reports || []} />
      </div>

      {/* Network Waterfall */}
      <div>
        <h2 className="text-xl font-semibold mb-4">
          Network Waterfall ({pageLoad.waterfall?.length || 0} resources)
        </h2>
        <NetworkWaterfall resources={pageLoad.waterfall || []} />
      </div>
    </div>
  );
}
