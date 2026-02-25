"use client";

import { useEffect, useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import {
  getWitnesServerAPI,
  type PageLoadDetailModel,
} from "../../generated/api";
import { Button } from "../ui/button";
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Clock,
  TrendingUp,
  Gauge,
  Route,
  AlertTriangle,
  Server,
  Monitor,
  ExternalLink,
} from "lucide-react";
import { NetworkWaterfall } from "../page-loads/NetworkWaterfall";
import { JankReports } from "../page-loads/JankReports";

interface PageLoadDetailPageProps {
  pageLoadId: string;
}

export default function PageLoadDetailPage({
  pageLoadId,
}: PageLoadDetailPageProps) {
  const { handleApiCall } = useApiToast();
  const [pageLoad, setPageLoad] = useState<PageLoadDetailModel | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPageLoadDetail = async () => {
      setLoading(true);
      const api = getWitnesServerAPI();
      await handleApiCall({
        apiCall: async () => {
          const response = await api.getV1PageLoadsId(pageLoadId);
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

  const navigateTo = (id: string) => {
    const currentUrl = window.location.pathname;
    const basePath = currentUrl.substring(0, currentUrl.lastIndexOf("/"));
    window.location.href = `${basePath}/${id}`;
  };

  const isSpaNav = pageLoad?.event_type === "SPA_NAV";

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
      {/* Incomplete Record Warning */}
      {pageLoad.incomplete && pageLoad.finalize_reason === "spa_nav" && (
        <div className="flex items-center gap-3 p-4 rounded-lg border border-amber-300 bg-amber-50 dark:border-amber-700 dark:bg-amber-950">
          <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400 shrink-0" />
          <p className="text-sm text-amber-800 dark:text-amber-200">
            Incomplete record — the user navigated away before this page finished loading. Metrics may be partial.
          </p>
        </div>
      )}

      {/* Navigation Controls */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            window.location.href = `/dashboard/visitors/${encodeURIComponent(pageLoad.user_id || "")}`;
          }}
        >
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Visitor
        </Button>
        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="sm"
            disabled={!pageLoad.previous_page_load_id}
            title={pageLoad.previous_url || undefined}
            onClick={() => {
              if (pageLoad.previous_page_load_id) {
                navigateTo(pageLoad.previous_page_load_id);
              }
            }}
          >
            <ChevronLeft className="h-4 w-4" />
            Prev
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={!pageLoad.next_page_load_id}
            title={pageLoad.next_url || undefined}
            onClick={() => {
              if (pageLoad.next_page_load_id) {
                navigateTo(pageLoad.next_page_load_id);
              }
            }}
          >
            Next
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Page Info Header */}
      <div className="space-y-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <h1 className="text-3xl font-bold tracking-tight">
              Page Load Details
            </h1>
            {isSpaNav ? (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200">
                <Route className="h-3 w-3" />
                SPA Route Change
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                Full Page Load
              </span>
            )}
          </div>
          <p className="text-muted-foreground break-all">{pageLoad.url}</p>
        </div>

        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-2">
            <Clock className="h-4 w-4" />
            {pageLoad.timestamp
              ? new Date(pageLoad.timestamp).toLocaleString()
              : "-"}
          </div>
          <div>User: {pageLoad.user_id}</div>
        </div>
      </div>

      {/* Section A — Server & Delivery */}
      <div>
        <div className="flex items-center gap-2 mb-4">
          <Server className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">Server & Delivery</h2>
        </div>
        {isSpaNav ? (
          <div className="p-6 border rounded-lg bg-muted/50">
            <p className="text-sm text-muted-foreground">
              Server metrics are only available for full page loads.{" "}
              {pageLoad.parent_page_load_id && (
                <a
                  href={`/dashboard/page-loads/${pageLoad.parent_page_load_id}`}
                  className="inline-flex items-center gap-1 text-primary hover:underline"
                >
                  See parent record
                  <ExternalLink className="h-3 w-3" />
                </a>
              )}
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
                <TrendingUp className="h-5 w-5 text-primary" />
                <h3 className="font-semibold">FCP</h3>
              </div>
              <p className="text-3xl font-bold">{pageLoad.fcp_ms}ms</p>
              <p className="text-sm text-muted-foreground">First Contentful Paint</p>
            </div>

            <div className="p-6 border rounded-lg">
              <div className="flex items-center gap-3 mb-2">
                <Clock className="h-5 w-5 text-primary" />
                <h3 className="font-semibold">DOM Interactive</h3>
              </div>
              <p className="text-3xl font-bold">{pageLoad.dom_interactive_ms}ms</p>
              <p className="text-sm text-muted-foreground">Page became interactive</p>
            </div>
          </div>
        )}
      </div>

      {/* Section B — User Experience */}
      <div>
        <div className="flex items-center gap-2 mb-4">
          <Monitor className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">User Experience</h2>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="p-6 border rounded-lg">
            <div className="flex items-center gap-3 mb-2">
              <TrendingUp className="h-5 w-5 text-primary" />
              <h3 className="font-semibold">
                {isSpaNav ? "Route Paint Time" : "LCP"}
              </h3>
            </div>
            <p className="text-3xl font-bold">
              {pageLoad.lcp_ms?.toFixed(0)}ms
            </p>
            <p className="text-sm text-muted-foreground">
              {isSpaNav
                ? "Time to render new route"
                : "Largest Contentful Paint"}
            </p>
          </div>

          <div className="p-6 border rounded-lg">
            <div className="flex items-center gap-3 mb-2">
              <Gauge className="h-5 w-5 text-primary" />
              <h3 className="font-semibold">CLS</h3>
            </div>
            <p className="text-3xl font-bold">{pageLoad.cls?.toFixed(3)}</p>
            <p className="text-sm text-muted-foreground">
              {isSpaNav
                ? "Layout shifts after route change"
                : "Cumulative Layout Shift"}
            </p>
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
        {isSpaNav && (
          <p className="text-sm text-muted-foreground mb-3">
            Showing resources loaded after route change only. Shell resources (JS, CSS, fonts) are in the{" "}
            {pageLoad.parent_page_load_id ? (
              <a
                href={`/dashboard/page-loads/${pageLoad.parent_page_load_id}`}
                className="text-primary hover:underline"
              >
                parent page load
              </a>
            ) : (
              "parent page load"
            )}
            .
          </p>
        )}
        <NetworkWaterfall resources={pageLoad.waterfall || []} />
        {!isSpaNav && pageLoad.has_spa_children && (
          <p className="text-sm text-muted-foreground mt-3">
            This page was subsequently navigated via SPA. Shell resources above were reused across SPA navigations.
          </p>
        )}
      </div>
    </div>
  );
}
