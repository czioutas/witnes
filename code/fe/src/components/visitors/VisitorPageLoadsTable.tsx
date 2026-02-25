"use client";

import React, { useEffect, useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import {
  getWitnesServerAPI,
  type PageLoadSummaryModel,
  ExperienceSymptom,
  OverallSentiment,
} from "../../generated/api";
import { TimeRangeFilter, type TimeRange } from "../filters/TimeRangeFilter";
import { Button } from "../ui/button";
import { Badge } from "../ui/badge";
import { Tooltip, TooltipTrigger, TooltipContent } from "../ui/tooltip";
import {
  ChevronRight,
  ChevronDown,
  ChevronUp,
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

const SYMPTOM_TOOLTIPS: Record<ExperienceSymptom, string> = {
  [ExperienceSymptom.slow_visual_load]: "The page took a long time before the visitor could see any content",
  [ExperienceSymptom.unstable_layout]: "Page elements shifted around while loading, causing a jumpy experience",
  [ExperienceSymptom.stuttering_ui]: "The page felt choppy or laggy while scrolling or interacting",
  [ExperienceSymptom.delayed_functional_ready]: "The page appeared ready but didn't respond to clicks for a while",
  [ExperienceSymptom.inifnite_waterfall]: "The page kept loading resources in the background and never fully settled",
  [ExperienceSymptom.rage_quit]: "The visitor left before the page became usable",
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

// --- Session grouping ---

interface SessionGroup {
  sessionId: string;
  referrerDisplay: string | null; // null = don't show (same-domain), "Direct", or "google.com"
  startedAt: Date;
  endedAt: Date;
  durationMs: number;
  pageCount: number;
  items: PageLoadSummaryModel[];
}

function getReferrerDisplay(
  sessionRef: string | null | undefined,
  pageUrl: string | null | undefined,
): string | null {
  if (!sessionRef) return "Direct";
  try {
    const refHost = new URL(sessionRef).hostname;
    if (pageUrl) {
      const pageHost = new URL(pageUrl).hostname;
      if (refHost === pageHost) return null; // same-domain, don't show
    }
    return refHost;
  } catch {
    return "Direct";
  }
}

function formatTimeBetween(ms: number): string {
  const hours = Math.floor(ms / 3_600_000);
  const days = Math.floor(ms / 86_400_000);

  if (days >= 1) return `${days} day${days > 1 ? "s" : ""} gap`;
  return `${hours} hour${hours > 1 ? "s" : ""} gap`;
}

function groupBySessions(items: PageLoadSummaryModel[]): SessionGroup[] {
  const sessionMap = new Map<string, PageLoadSummaryModel[]>();

  // Group items by sessionId, preserving order
  for (const item of items) {
    const sid = item.session_id ?? item.id ?? "unknown";
    if (!sessionMap.has(sid)) sessionMap.set(sid, []);
    sessionMap.get(sid)!.push(item);
  }

  const sessions: SessionGroup[] = [];
  for (const [sessionId, sessionItems] of sessionMap) {
    // Sort items within session reverse-chronologically (DESC — most recent first)
    sessionItems.sort(
      (a, b) =>
        new Date(b.page_requested_at_by_visitor ?? 0).getTime() -
        new Date(a.page_requested_at_by_visitor ?? 0).getTime(),
    );

    // Items are sorted DESC so first = newest, last = oldest
    const oldest = sessionItems[sessionItems.length - 1];
    const newest = sessionItems[0];
    const startedAt = new Date(oldest.page_requested_at_by_visitor ?? 0);
    const endedAt = new Date(newest.page_requested_at_by_visitor ?? 0);

    // Find the first LOAD in session (chronologically) for referrer display
    const firstLoad = sessionItems.findLast(
      (i) => i.event_type !== "SPA_NAV",
    );
    const refItem = firstLoad ?? oldest;

    sessions.push({
      sessionId,
      referrerDisplay: getReferrerDisplay(
        refItem.session_ref,
        refItem.url_path,
      ),
      startedAt,
      endedAt,
      durationMs: endedAt.getTime() - startedAt.getTime(),
      pageCount: sessionItems.length,
      items: sessionItems,
    });
  }

  // Sort sessions by most recent first (DESC by startedAt)
  sessions.sort((a, b) => b.startedAt.getTime() - a.startedAt.getTime());
  return sessions;
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
  sentiment,
}: {
  icon: React.ElementType;
  isIssue?: boolean;
  label: string;
  okLabel: string;
  reasons?: string[];
  sentiment?: OverallSentiment;
}) {
  const hasReasons = reasons && reasons.length > 0;
  const issueColor =
    sentiment === OverallSentiment.good
      ? "text-yellow-500"
      : "text-red-500";
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <div className="w-8 shrink-0 flex justify-center">
          <IconComponent
            className={`h-4 w-4 ${isIssue ? issueColor : "text-muted-foreground/40"}`}
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
  const [allItems, setAllItems] = useState<PageLoadSummaryModel[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);

  const [timeRange, setTimeRange] = useState<TimeRange>({ preset: "all" });
  const nextPageRef = React.useRef(1);
  const pageSize = 40;

  const fetchPageLoads = async (
    page: number,
    range: TimeRange = timeRange,
    reset: boolean = false,
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
        const newItems = data.data ?? [];
        setHasMore(data.has_next_page ?? false);
        nextPageRef.current = page + 1;

        if (reset) {
          setAllItems(newItems);
        } else {
          // Accumulate and deduplicate by ID
          setAllItems((prev) => {
            const seen = new Set(prev.map((i) => i.id));
            const unique = newItems.filter((i) => !seen.has(i.id));
            return [...prev, ...unique];
          });
        }
      },
      onError: () => setLoading(false),
    });
    setLoading(false);
  };

  const handleLoadMore = () => {
    fetchPageLoads(nextPageRef.current);
  };

  const handleTimeRangeChange = (newRange: TimeRange) => {
    setTimeRange(newRange);
    nextPageRef.current = 1;
    fetchPageLoads(1, newRange, true);
  };

  const handleClearFilter = () => {
    const resetRange: TimeRange = { preset: "all" };
    setTimeRange(resetRange);
    nextPageRef.current = 1;
    fetchPageLoads(1, resetRange, true);
  };

  // Initial load
  useEffect(() => {
    fetchPageLoads(1, timeRange, true);
  }, [userId]);

  const items = allItems;
  const sessions = groupBySessions(items);
  const [collapsedSessions, setCollapsedSessions] = useState<Set<string>>(
    new Set(),
  );

  const toggleSession = (sessionId: string) => {
    setCollapsedSessions((prev) => {
      const next = new Set(prev);
      if (next.has(sessionId)) next.delete(sessionId);
      else next.add(sessionId);
      return next;
    });
  };

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
      {loading && items.length === 0 && (
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
        <div className="space-y-4">
          {sessions.map((session, sessionIdx) => {
            const isCollapsed = collapsedSessions.has(session.sessionId);
            const prevSession = sessionIdx > 0 ? sessions[sessionIdx - 1] : null;
            const gapMs = prevSession
              ? prevSession.startedAt.getTime() - session.endedAt.getTime()
              : null;

            return (
              <div key={session.sessionId}>
                {/* Time gap label between sessions */}
                {gapMs != null && gapMs > 3_600_000 && (
                  <div className="flex items-center gap-3 py-2">
                    <div className="flex-1 border-t border-dashed" />
                    <span className="text-xs text-muted-foreground">
                      {formatTimeBetween(gapMs)}
                    </span>
                    <div className="flex-1 border-t border-dashed" />
                  </div>
                )}

                {/* Session header */}
                <button
                  type="button"
                  className="w-full flex items-center gap-3 py-2 px-3 rounded-lg bg-muted/30 hover:bg-muted/50 transition-colors text-left"
                  onClick={() => toggleSession(session.sessionId)}
                >
                  {isCollapsed ? (
                    <ChevronRight className="h-4 w-4 text-muted-foreground shrink-0" />
                  ) : (
                    <ChevronDown className="h-4 w-4 text-muted-foreground shrink-0" />
                  )}
                  <span className="text-sm font-medium">
                    {session.startedAt.toLocaleDateString(undefined, {
                      weekday: "short",
                      month: "short",
                      day: "numeric",
                    })}{" "}
                    at{" "}
                    {session.startedAt.toLocaleTimeString(undefined, {
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </span>
                  <span className="text-xs text-muted-foreground ml-auto">
                    {session.pageCount} navigation{session.pageCount !== 1 ? "s" : ""}
                  </span>
                </button>

                {/* Session rows */}
                {!isCollapsed && (
                  <div className="space-y-2 mt-2 ml-4 border-l-2 border-muted pl-3">
                    {session.items.map((pl, plIdx) => {
                      const isLatest = plIdx === 0;
                      const pageLoadUrl = `/dashboard/page-loads/${pl.silver_id}`;
                      const speed = speedLabel(pl.total_initial_load_ms ?? 0);
                      const settledDelta =
                        pl.settled_time_ms != null &&
                        pl.total_initial_load_ms != null &&
                        pl.settled_time_ms > pl.total_initial_load_ms * 1.5
                          ? pl.settled_time_ms - pl.total_initial_load_ms
                          : null;

                      // Time gap between this item and the one above (items are DESC)
                      let gapLabel: string | null = null;
                      if (plIdx > 0) {
                        const prevTime = new Date(session.items[plIdx - 1].page_requested_at_by_visitor ?? 0).getTime();
                        const currTime = new Date(pl.page_requested_at_by_visitor ?? 0).getTime();
                        const gapMs = prevTime - currTime;
                        const gapHours = gapMs / (1000 * 60 * 60);
                        if (gapHours >= 24) {
                          const days = Math.floor(gapHours / 24);
                          gapLabel = `${days} day${days !== 1 ? "s" : ""} gap`;
                        } else if (gapHours >= 1) {
                          const hours = Math.floor(gapHours);
                          gapLabel = `${hours} hour${hours !== 1 ? "s" : ""} gap`;
                        }
                      }

                      return (
                        <React.Fragment key={pl.id}>
                        {gapLabel && (
                          <div className="flex items-center gap-3 py-1">
                            <div className="flex-1 border-t border-dashed border-muted-foreground/30" />
                            <span className="text-[11px] text-muted-foreground whitespace-nowrap">
                              {gapLabel}
                            </span>
                            <div className="flex-1 border-t border-dashed border-muted-foreground/30" />
                          </div>
                        )}
                        <div
                          className="group flex items-center gap-3 rounded-lg border p-3 cursor-pointer transition-colors hover:bg-muted/50"
                          onClick={() => (window.location.href = pageLoadUrl)}
                          onAuxClick={(e) => {
                            if (e.button === 1) {
                              e.preventDefault();
                              window.open(pageLoadUrl, "_blank");
                            }
                          }}
                        >
                          {/* Sentiment dot */}
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

                          {isLatest && (
                            <span className="shrink-0 inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
                              Latest
                            </span>
                          )}

                          {/* URL + DateTime + device/browser */}
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
                                  <Tooltip key={symptom}>
                                    <TooltipTrigger asChild>
                                      <Badge
                                        variant="destructive"
                                        className="text-[10px] px-1.5 py-0"
                                      >
                                        {SYMPTOM_LABELS[symptom] ?? symptom}
                                      </Badge>
                                    </TooltipTrigger>
                                    <TooltipContent>
                                      {SYMPTOM_TOOLTIPS[symptom]}
                                    </TooltipContent>
                                  </Tooltip>
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
                                  User navigated away before page finished loading
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

                          {/* Performance */}
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
                              sentiment={pl.overall_sentiment}
                            />
                            <PillarIcon
                              icon={Wifi}
                              isIssue={pl.is_network_issue}
                              label="Network Issue"
                              okLabel="Network OK"
                              reasons={pl.network_reasons}
                              sentiment={pl.overall_sentiment}
                            />
                            <PillarIcon
                              icon={Code}
                              isIssue={pl.is_frontend_issue}
                              label="Frontend Issue"
                              okLabel="Frontend OK"
                              reasons={pl.frontend_reasons}
                              sentiment={pl.overall_sentiment}
                            />
                            <PillarIcon
                              icon={Package}
                              isIssue={pl.is_payload_issue}
                              label="Payload Issue"
                              okLabel="Payload OK"
                              reasons={pl.payload_reasons}
                              sentiment={pl.overall_sentiment}
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
                        </React.Fragment>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Load more */}
      {hasMore && (
        <div className="flex items-center justify-center pt-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleLoadMore}
            disabled={loading}
          >
            {loading ? "Loading..." : "Load more"}
          </Button>
        </div>
      )}
    </div>
  );
}
