"use client";

import { useEffect, useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import {
  getWitnesServerAPI,
  type PageLoadSummaryModel,
  type PageLoadSummaryModelPagedResult,
  ExperienceSymptom,
  OverallSentiment,
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
  Package,
  ExternalLink,
  AlertTriangle,
  DoorOpen,
} from "lucide-react";
import { SiGooglechrome, SiSafari, SiFirefox, SiOpera } from "react-icons/si";

interface VisitorPageLoadsTableProps {
  userId: string;
}

// --- Experience symptom labels ---

const SYMPTOM_LABELS: Record<ExperienceSymptom, string> = {
  [ExperienceSymptom.slow_visual_load]: "Slow Load",
  [ExperienceSymptom.unstable_layout]: "Unstable",
  [ExperienceSymptom.stuttering_ui]: "Jittery",
  [ExperienceSymptom.delayed_functional_ready]: "Delayed",
  [ExperienceSymptom.inifnite_waterfall]: "Stalled",
  [ExperienceSymptom.rage_quit]: "Aborted",
};

// --- Helpers ---

function humanizeReason(reason: string): string {
  return reason.replace(/_/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
}

function formatDuration(ms: number): string {
  if (ms >= 1000) return `${(ms / 1000).toFixed(1)}s`;
  return `${ms}ms`;
}

function speedLabel(ms: number): { text: string; color: string } {
  if (ms < 1000)
    return { text: "Fast", color: "text-green-600 dark:text-green-400" };
  if (ms <= 2500)
    return { text: "Moderate", color: "text-yellow-600 dark:text-yellow-400" };
  return { text: "Slow", color: "text-red-600 dark:text-red-400" };
}

function sentimentDot(sentiment?: OverallSentiment): {
  color: string;
  label: string;
} {
  switch (sentiment) {
    case OverallSentiment.good:
      return { color: "bg-green-500", label: "Good experience" };
    case OverallSentiment.bad:
      return { color: "bg-red-500", label: "Bad experience" };
    default:
      return { color: "bg-yellow-500", label: "Neutral experience" };
  }
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
    const day = item.page_requested_at_by_visitor
      ? new Date(item.page_requested_at_by_visitor).toLocaleDateString(
          undefined,
          {
            weekday: "long",
            year: "numeric",
            month: "long",
            day: "numeric",
          },
        )
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

// --- Pillar icon with reasons tooltip ---

function PillarIcon({
  icon: IconComponent,
  isIssue,
  label,
  okLabel,
  reasons,
}: {
  icon: React.ElementType;
  isIssue?: boolean;
  label: string;
  okLabel: string;
  reasons?: string[];
}) {
  const hasReasons = reasons && reasons.length > 0;
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <div className="w-8 shrink-0 flex justify-center">
          <IconComponent
            className={`h-4 w-4 ${isIssue ? "text-red-500" : "text-muted-foreground/40"}`}
          />
        </div>
      </TooltipTrigger>
      <TooltipContent>
        {isIssue ? (
          <div>
            <p className="font-medium">{label}</p>
            {hasReasons && (
              <ul className="mt-1 text-xs space-y-0.5">
                {reasons.map((r) => (
                  <li key={r}>• {humanizeReason(r)}</li>
                ))}
              </ul>
            )}
          </div>
        ) : (
          okLabel
        )}
      </TooltipContent>
    </Tooltip>
  );
}

// --- Main component ---

export function VisitorPageLoadsTable({ userId }: VisitorPageLoadsTableProps) {
  const { handleApiCall } = useApiToast();
  const [pageLoads, setPageLoads] =
    useState<PageLoadSummaryModelPagedResult | null>(null);
  const [loading, setLoading] = useState(false);

  const [timeRange, setTimeRange] = useState<TimeRange>({ preset: "all" });
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;

  const fetchPageLoads = async (
    page: number = currentPage,
    range: TimeRange = timeRange,
  ) => {
    setLoading(true);
    const api = getWitnesServerAPI();
    await handleApiCall({
      apiCall: async () => {
        const response = await api.getV1VisitorsUserIdPageLoads(userId, {
          startDate: range.startDate?.toISOString(),
          endDate: range.endDate?.toISOString(),
          pageNumber: page,
          pageSize: pageSize,
        });
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
    fetchPageLoads(1, newRange);
  };

  const handleClearFilter = () => {
    const resetRange: TimeRange = { preset: "all" };
    setTimeRange(resetRange);
    setCurrentPage(1);
    fetchPageLoads(1, resetRange);
  };

  // Initial load
  useEffect(() => {
    fetchPageLoads();
  }, [userId]);

  const items = pageLoads?.data ?? [];
  const grouped = groupByDay(items);
  const hasActiveFilter =
    timeRange.preset !== "all" ||
    !!timeRange.startDate ||
    !!timeRange.endDate ||
    !!timeRange.aroundTime;

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex items-center justify-end">
        <TimeRangeFilter
          value={timeRange}
          onChange={handleTimeRangeChange}
          showQuickPresets={false}
        />
      </div>

      {/* Timeline */}
      {loading && !pageLoads && (
        <div className="text-center py-8 text-muted-foreground">Loading...</div>
      )}

      {!loading && items.length === 0 && (
        <div className="text-center py-8 text-muted-foreground">
          <span>No page loads found</span>
          {hasActiveFilter && (
            <>
              <span className="mx-2">•</span>
              <button
                type="button"
                onClick={handleClearFilter}
                className="underline underline-offset-4 hover:text-foreground"
              >
                Clear filter
              </button>
            </>
          )}
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
                  const speed = speedLabel(pl.total_initial_load_ms ?? 0);
                  const settledDelta =
                    pl.settled_time_ms != null &&
                    pl.total_initial_load_ms != null &&
                    pl.settled_time_ms > pl.total_initial_load_ms * 1.5
                      ? pl.settled_time_ms - pl.total_initial_load_ms
                      : null;

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
                      {/* Left: Sentiment dot */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="shrink-0">
                            <span
                              className={`inline-block h-3 w-3 rounded-full ${sentimentDot(pl.overall_sentiment).color}`}
                            />
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          {sentimentDot(pl.overall_sentiment).label}
                        </TooltipContent>
                      </Tooltip>

                      {/* Left: URL + DateTime + device/browser */}
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium truncate">
                          {pl.url_path || "/"}
                        </p>
                        <div className="flex items-center gap-2 mt-0.5">
                          <span className="text-xs text-muted-foreground">
                            {formatDateTime(pl.page_requested_at_by_visitor)}
                          </span>
                          <DeviceIcon icon={pl.device_icon} />
                          <BrowserIconDisplay icon={pl.browser_icon} />
                        </div>
                      </div>
                      <div className="shrink-0 flex items-center gap-1.5">
                        {pl.experience_symptoms &&
                          pl.experience_symptoms.length > 0 &&
                          pl.experience_symptoms
                            .filter((s) => s !== ExperienceSymptom.rage_quit)
                            .map((symptom) => (
                              <Badge
                                key={symptom}
                                variant="destructive"
                                className="text-[10px] px-1.5 py-0"
                              >
                                {SYMPTOM_LABELS[symptom] ?? symptom}
                              </Badge>
                            ))}
                        {pl.incomplete && (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Badge
                                variant="outline"
                                className="text-[10px] px-1.5 py-0 text-amber-600 border-amber-400"
                              >
                                <AlertTriangle className="h-3 w-3 mr-0.5" />
                                Partial Data
                              </Badge>
                            </TooltipTrigger>
                            <TooltipContent>
                              Metrics may be incomplete — not all data was
                              captured for this page load
                            </TooltipContent>
                          </Tooltip>
                        )}
                        {pl.user_left_early && (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Badge
                                variant="outline"
                                className="text-[10px] px-1.5 py-0 text-orange-600 border-orange-400"
                              >
                                <DoorOpen className="h-3 w-3 mr-0.5" />
                                User Exited
                              </Badge>
                            </TooltipTrigger>
                            <TooltipContent>
                              User navigated away before the page finished
                              loading
                            </TooltipContent>
                          </Tooltip>
                        )}
                      </div>

                      {/* Performance: Initial Load + Settled delta */}
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <div className="w-28 shrink-0 text-right">
                            <span
                              className={`text-sm font-medium tabular-nums ${speed.color}`}
                            >
                              {speed.text}
                            </span>
                            <span className="text-xs text-muted-foreground tabular-nums ml-1">
                              {formatDuration(pl.total_initial_load_ms ?? 0)}
                            </span>
                            {settledDelta != null && (
                              <p className="text-[10px] text-muted-foreground tabular-nums">
                                +{formatDuration(settledDelta)} settling
                              </p>
                            )}
                          </div>
                        </TooltipTrigger>
                        <TooltipContent>
                          <div className="space-y-0.5">
                            <p>
                              Initial Load:{" "}
                              {formatDuration(pl.total_initial_load_ms ?? 0)}
                            </p>
                            {pl.absolute_lcp_ms != null &&
                              pl.absolute_lcp_ms > 0 && (
                                <p>LCP: {formatDuration(pl.absolute_lcp_ms)}</p>
                              )}
                            {pl.cls_score != null && (
                              <p>CLS: {pl.cls_score.toFixed(3)}</p>
                            )}
                            {pl.settled_time_ms != null && (
                              <p>
                                Fully Settled:{" "}
                                {formatDuration(pl.settled_time_ms)}
                              </p>
                            )}
                            {pl.connection_quality && (
                              <p>Connection: {pl.connection_quality}</p>
                            )}
                          </div>
                        </TooltipContent>
                      </Tooltip>

                      {/* 4-Pillar Attribution */}
                      <div className="shrink-0 flex items-center gap-0.5">
                        <PillarIcon
                          icon={Server}
                          isIssue={pl.is_backend_issue}
                          label="Backend Issue"
                          okLabel="Backend OK"
                          reasons={pl.backend_reasons}
                        />
                        <PillarIcon
                          icon={Wifi}
                          isIssue={pl.is_network_issue}
                          label="Network Issue"
                          okLabel="Network OK"
                          reasons={pl.network_reasons}
                        />
                        <PillarIcon
                          icon={Code}
                          isIssue={pl.is_frontend_issue}
                          label="Frontend Issue"
                          okLabel="Frontend OK"
                          reasons={pl.frontend_reasons}
                        />
                        <PillarIcon
                          icon={Package}
                          isIssue={pl.is_payload_issue}
                          label="Payload Issue"
                          okLabel="Payload OK"
                          reasons={pl.payload_reasons}
                        />
                      </div>

                      {/* Open in new tab */}
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
