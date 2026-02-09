"use client";

import { useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import {
  getWitnesServerAPI,
  type PageLoadSummaryModel,
  type PageLoadSummaryModelPagedResult,
} from "../../generated/api";
import { TimeRangeFilter, type TimeRange } from "../filters/TimeRangeFilter";
import { Button } from "../ui/button";
import { Badge } from "../ui/badge";
import { Tooltip, TooltipTrigger, TooltipContent } from "../ui/tooltip";
import {
  ChevronLeft,
  ChevronRight,
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  Wifi,
  Server,
  Code,
  ExternalLink,
  AlertTriangle,
} from "lucide-react";
import { SiGooglechrome, SiSafari, SiFirefox, SiOpera } from "react-icons/si";

interface UserPageLoadsTableProps {
  userId: string;
}

// --- Verdict helpers ---

function lcpDot(verdict?: string) {
  switch (verdict) {
    case "Fast":
      return "bg-green-500";
    case "Average":
      return "bg-yellow-500";
    case "Slow":
      return "bg-red-500";
    default:
      return "bg-muted-foreground";
  }
}

function clsBadgeVariant(
  verdict?: string,
): "default" | "secondary" | "destructive" | "outline" {
  return verdict === "Shifty" ? "destructive" : "secondary";
}

// --- Device / Browser icons ---

function DeviceIcon({ icon }: { icon?: string }) {
  const cls = "h-4 w-4 text-muted-foreground";
  switch (icon?.toLowerCase()) {
    case "mobile":
      return <Smartphone className={cls} />;
    case "tablet":
      return <Tablet className={cls} />;
    default:
      return <Monitor className={cls} />;
  }
}

function BrowserIconDisplay({ icon }: { icon?: string }) {
  const cls = "h-4 w-4 text-muted-foreground";
  const label = icon ?? "Unknown";

  let iconEl: React.ReactNode;
  switch (icon?.toLowerCase()) {
    case "chrome":
      iconEl = <SiGooglechrome className={cls} />;
      break;
    case "safari":
      iconEl = <SiSafari className={cls} />;
      break;
    case "firefox":
      iconEl = <SiFirefox className={cls} />;
      break;
    case "edge":
      iconEl = <Globe className={cls} />;
      break;
    case "opera":
      iconEl = <SiOpera className={cls} />;
      break;
    default:
      iconEl = <Globe className={cls} />;
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <span className="inline-flex">{iconEl}</span>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}

// --- Date grouping ---

function groupByDay(
  items: PageLoadSummaryModel[],
): Map<string, PageLoadSummaryModel[]> {
  const groups = new Map<string, PageLoadSummaryModel[]>();
  for (const item of items) {
    const day = item.timestamp
      ? new Date(item.timestamp).toLocaleDateString(undefined, {
          weekday: "long",
          year: "numeric",
          month: "long",
          day: "numeric",
        })
      : "Unknown Date";
    if (!groups.has(day)) groups.set(day, []);
    groups.get(day)!.push(item);
  }
  return groups;
}

function formatDateTime(timestamp?: string) {
  if (!timestamp) return "-";
  const d = new Date(timestamp);
  const day = d.toLocaleDateString(undefined, {
    weekday: "short",
    month: "short",
    day: "numeric",
  });
  const time = d.toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
  return `${day} at ${time}`;
}

// --- Main component ---

export function UserPageLoadsTable({ userId }: UserPageLoadsTableProps) {
  const { handleApiCall } = useApiToast();
  const [pageLoads, setPageLoads] =
    useState<PageLoadSummaryModelPagedResult | null>(null);
  const [loading, setLoading] = useState(false);

  const [timeRange, setTimeRange] = useState<TimeRange>({ preset: "all" });
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;

  const fetchPageLoads = async (page: number = currentPage) => {
    setLoading(true);
    const api = getWitnesServerAPI();
    await handleApiCall({
      apiCall: async () => {
        const response = await api.getApiV1VisitorsUserIdPageLoads(
          userId,
          {
            startDate: timeRange.startDate?.toISOString(),
            endDate: timeRange.endDate?.toISOString(),
            pageNumber: page,
            pageSize: pageSize,
          },
        );
        return response.data;
      },
      onSuccess: (data) => {
        setPageLoads(data);
        setCurrentPage(page);
      },
      onError: () => setLoading(false),
    });
    setLoading(false);
  };

  const handleTimeRangeChange = (newRange: TimeRange) => {
    setTimeRange(newRange);
    setCurrentPage(1);
    setTimeout(() => fetchPageLoads(1), 100);
  };

  // Initial load
  useState(() => {
    fetchPageLoads();
  });

  const items = pageLoads?.data ?? [];
  const grouped = groupByDay(items);

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex items-center justify-end">
        <TimeRangeFilter value={timeRange} onChange={handleTimeRangeChange} />
      </div>

      {/* Timeline */}
      {loading && !pageLoads && (
        <div className="text-center py-8 text-muted-foreground">Loading...</div>
      )}

      {!loading && items.length === 0 && (
        <div className="text-center py-8 text-muted-foreground">
          No page loads found
        </div>
      )}

      {items.length > 0 && (
        <div className="space-y-6">
          {[...grouped.entries()].map(([day, loads]) => (
            <div key={day}>
              {/* Day header */}
              <div className="sticky top-0 z-10 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 py-2 mb-2">
                <h3 className="text-sm font-medium text-muted-foreground">
                  {day}
                </h3>
              </div>

              {/* Rows for this day */}
              <div className="space-y-2">
                {loads.map((pl) => {
                  const pageLoadUrl = `/dashboard/page-loads/${pl.silver_id}`;
                  return (
                    <div
                      key={pl.id}
                      className="group flex items-center gap-3 rounded-lg border p-3 cursor-pointer transition-colors hover:bg-muted/50"
                      onClick={() => (window.location.href = pageLoadUrl)}
                      onAuxClick={(e) => {
                        if (e.button === 1) {
                          e.preventDefault();
                          window.open(pageLoadUrl, "_blank");
                        }
                      }}
                    >
                      {/* Col 1: Status dot */}
                      <div className="shrink-0">
                        <span
                          className={`inline-block h-3 w-3 rounded-full ${lcpDot(pl.lcp_verdict)}`}
                        />
                      </div>

                      {/* Col 2: URL (row 1) + DateTime (row 2) */}
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium truncate">
                          {pl.url_path || "/"}
                        </p>
                        <div className="flex items-center gap-2 mt-0.5">
                          <span className="text-xs text-muted-foreground">
                            {formatDateTime(pl.timestamp)}
                          </span>
                          <DeviceIcon icon={pl.device_icon} />
                          <BrowserIconDisplay icon={pl.browser_icon} />
                        </div>
                      </div>

                      {/* Col 3: Incomplete warning + Load Speed */}
                      {pl.incomplete && (
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <div className="shrink-0 flex justify-center">
                              <AlertTriangle className="h-4 w-4 text-amber-500" />
                            </div>
                          </TooltipTrigger>
                          <TooltipContent>
                            Potentially Incomplete — user navigated away in less
                            than 2.5 seconds after page load, which may lead to
                            missing performance metrics
                          </TooltipContent>
                        </Tooltip>
                      )}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="w-24 shrink-0 text-right">
                            <span className="text-sm font-medium tabular-nums">
                              {pl.lcp_verdict ?? "—"}
                            </span>
                            <span className="text-xs text-muted-foreground tabular-nums ml-1">
                              {pl.lcp_ms ?? 0}ms
                            </span>
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          Load Speed (LCP): {pl.lcp_ms ?? 0}ms
                        </TooltipContent>
                      </Tooltip>

                      {/* Col 4: Stability (fixed width) */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="w-16 shrink-0 flex justify-center">
                            <Badge variant={clsBadgeVariant(pl.cls_verdict)}>
                              {pl.cls_verdict ?? "Solid"}
                            </Badge>
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          Visual Stability (CLS):{" "}
                          {pl.cls_score?.toFixed(3) ?? "0.000"}
                        </TooltipContent>
                      </Tooltip>

                      {/* Col 5: TTFB (fixed width) */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <span className="w-20 shrink-0 text-right text-xs text-muted-foreground tabular-nums">
                            {pl.ttfb_ms ?? 0}ms TTFB
                          </span>
                        </TooltipTrigger>
                        <TooltipContent>
                          Wait for Data (TTFB): {pl.ttfb_ms ?? 0}ms
                        </TooltipContent>
                      </Tooltip>

                      {/* Col 6: Connection fault */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="w-8 shrink-0 flex justify-center">
                            <Wifi
                              className={`h-4 w-4 ${pl.is_connection_fault ? "text-red-500" : "text-green-500"}`}
                            />
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          {pl.is_connection_fault
                            ? "Slow Connection"
                            : "Connection OK"}
                        </TooltipContent>
                      </Tooltip>

                      {/* Col 7: Backend fault */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="w-8 shrink-0 flex justify-center">
                            <Server
                              className={`h-4 w-4 ${pl.is_backend_fault ? "text-red-500" : "text-green-500"}`}
                            />
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          {pl.is_backend_fault ? "API Latency" : "Backend OK"}
                        </TooltipContent>
                      </Tooltip>

                      {/* Col 8: Frontend fault */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="w-8 shrink-0 flex justify-center">
                            <Code
                              className={`h-4 w-4 ${pl.is_frontend_fault ? "text-red-500" : "text-green-500"}`}
                            />
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          {pl.is_frontend_fault ? "Heavy Page" : "Frontend OK"}
                        </TooltipContent>
                      </Tooltip>

                      {/* Col 9: Open in new tab */}
                      <a
                        href={pageLoadUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        onClick={(e) => e.stopPropagation()}
                        className="shrink-0 ml-1 inline-flex items-center justify-center rounded-md p-1 text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                        title="Open in new tab"
                      >
                        <ExternalLink className="h-4 w-4" />
                      </a>
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Pagination */}
      {pageLoads && pageLoads.total_count! > 0 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            Showing {(pageLoads.page_number! - 1) * pageLoads.page_size! + 1} to{" "}
            {Math.min(
              pageLoads.page_number! * pageLoads.page_size!,
              pageLoads.total_count!,
            )}{" "}
            of {pageLoads.total_count} results
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => fetchPageLoads(currentPage - 1)}
              disabled={!pageLoads.has_previous_page || loading}
            >
              <ChevronLeft className="h-4 w-4" />
              Previous
            </Button>
            <div className="text-sm">
              Page {pageLoads.page_number} of {pageLoads.total_pages}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => fetchPageLoads(currentPage + 1)}
              disabled={!pageLoads.has_next_page || loading}
            >
              Next
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
