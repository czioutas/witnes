"use client";

import { useState } from "react";
import { useApiToast } from "../../hooks/useApiToast";
import {
  getWitnesServerAPI,
  type VisitorSummaryModel,
  type VisitorSummaryModelPagedResult,
} from "../../generated/api";
import { TimeRangeFilter, type TimeRange } from "../filters/TimeRangeFilter";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../ui/table";
import { Search, ChevronLeft, ChevronRight, ExternalLink } from "lucide-react";

export function VisitorsTable() {
  const { handleApiCall } = useApiToast();
  const [visitors, setVisitors] =
    useState<VisitorSummaryModelPagedResult | null>(null);
  const [loading, setLoading] = useState(false);

  // Filters
  const [userIdSearch, setUserIdSearch] = useState("");
  const [timeRange, setTimeRange] = useState<TimeRange>({ preset: "all" });
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;

  const fetchVisitors = async (page: number = currentPage) => {
    setLoading(true);
    const api = getWitnesServerAPI();
    await handleApiCall({
      apiCall: async () => {
        const response = await api.getApiV1Visitors({
          UserIdSearch: userIdSearch || undefined,
          StartDate: timeRange.startDate?.toISOString(),
          EndDate: timeRange.endDate?.toISOString(),
          PageNumber: page,
          PageSize: pageSize,
        });
        return response.data;
      },
      onSuccess: (data) => {
        setVisitors(data);
        setCurrentPage(page);
      },
      onError: () => setLoading(false),
    });
    setLoading(false);
  };

  const handleSearch = () => {
    setCurrentPage(1);
    fetchVisitors(1);
  };

  const handleTimeRangeChange = (newRange: TimeRange) => {
    setTimeRange(newRange);
    setCurrentPage(1);
    // Auto-fetch when time range changes
    setTimeout(() => fetchVisitors(1), 100);
  };

  // Initial load
  useState(() => {
    fetchVisitors();
  });

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex items-center gap-4">
        <div className="flex-1 flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search by User ID..."
              value={userIdSearch}
              onChange={(e) => setUserIdSearch(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              className="pl-9"
            />
          </div>
          <Button onClick={handleSearch} disabled={loading}>
            Search
          </Button>
        </div>
        <TimeRangeFilter value={timeRange} onChange={handleTimeRangeChange} />
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>User ID</TableHead>
              <TableHead>Total Page Loads</TableHead>
              <TableHead>Last Seen</TableHead>
              <TableHead>Browsers</TableHead>
              <TableHead>Operating Systems</TableHead>
              <TableHead className="w-10"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && !visitors && (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-8">
                  Loading...
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              visitors &&
              visitors.data &&
              visitors.data.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="text-center py-8 text-muted-foreground"
                  >
                    No visitors found
                  </TableCell>
                </TableRow>
              )}
            {visitors?.data?.map((visitor: VisitorSummaryModel) => {
              const displayId = visitor.user_id || visitor.guest_id || "Unknown";
              const isGuest = !visitor.user_id && !!visitor.guest_id;
              const visitorUrl = `/dashboard/visitors/${encodeURIComponent(displayId)}`;
              return (
                <TableRow
                  key={visitor.user_id || visitor.guest_id}
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() => window.location.href = visitorUrl}
                  onAuxClick={(e) => {
                    if (e.button === 1) {
                      e.preventDefault();
                      window.open(visitorUrl, "_blank");
                    }
                  }}
                >
                  <TableCell className="font-medium">
                    {displayId}
                    {isGuest && (
                      <span className="ml-2 text-xs px-1.5 py-0.5 rounded bg-muted text-muted-foreground">Guest</span>
                    )}
                  </TableCell>
                  <TableCell>{visitor.total_page_loads}</TableCell>
                  <TableCell>
                    {visitor.last_seen_at
                      ? new Date(visitor.last_seen_at).toLocaleString()
                      : "-"}
                  </TableCell>
                  <TableCell>
                    {visitor.browsers && visitor.browsers.length > 0
                      ? visitor.browsers.join(", ")
                      : "-"}
                  </TableCell>
                  <TableCell>
                    {visitor.operating_systems &&
                    visitor.operating_systems.length > 0
                      ? visitor.operating_systems.join(", ")
                      : "-"}
                  </TableCell>
                  <TableCell>
                    <a
                      href={visitorUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      onClick={(e) => e.stopPropagation()}
                      className="inline-flex items-center justify-center rounded-md p-1 text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                      title="Open in new tab"
                    >
                      <ExternalLink className="h-4 w-4" />
                    </a>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {visitors && visitors.total_count! > 0 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            Showing {(visitors.page_number! - 1) * visitors.page_size! + 1} to{" "}
            {Math.min(
              visitors.page_number! * visitors.page_size!,
              visitors.total_count!,
            )}{" "}
            of {visitors.total_count} results
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => fetchVisitors(currentPage - 1)}
              disabled={!visitors.has_previous_page || loading}
            >
              <ChevronLeft className="h-4 w-4" />
              Previous
            </Button>
            <div className="text-sm">
              Page {visitors.page_number} of {visitors.total_pages}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => fetchVisitors(currentPage + 1)}
              disabled={!visitors.has_next_page || loading}
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
