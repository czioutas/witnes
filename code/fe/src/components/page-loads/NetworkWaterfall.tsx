import type { ResourceTimingModel } from "../../generated/api";

interface NetworkWaterfallProps {
  resources: ResourceTimingModel[];
}

export function NetworkWaterfall({ resources }: NetworkWaterfallProps) {
  if (!resources || resources.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        No network resources recorded
      </div>
    );
  }

  // Find the slowest XHR/Fetch request for the critical request callout
  const criticalRequest = resources
    .filter(
      (r) =>
        r.label &&
        (r.label.includes("FETCH") || r.label.includes("XMLHTTPREQUEST")),
    )
    .sort((a, b) => (b.total_ms || 0) - (a.total_ms || 0))[0];

  const criticalRequestName = criticalRequest?.label
    ?.split("|")
    .slice(1)
    .join("|")
    .trim();

  const maxTotalMs = Math.max(...resources.map((r) => r.total_ms || 0));

  return (
    <div className="space-y-2">
      {/* Critical Request Callout */}
      {criticalRequest && (criticalRequest.total_ms || 0) > 0 && (
        <div className="mb-4 p-4 border border-amber-300 dark:border-amber-700 rounded-lg bg-amber-50 dark:bg-amber-950">
          <p className="text-xs font-semibold text-amber-800 dark:text-amber-200 uppercase tracking-wide mb-1">
            Critical Request
          </p>
          <div className="flex items-center gap-3 text-sm">
            <span className="font-mono text-xs truncate max-w-md text-amber-900 dark:text-amber-100">
              {criticalRequestName || criticalRequest.full_url}
            </span>
            <span className="font-semibold text-amber-800 dark:text-amber-200">
              {criticalRequest.total_ms}ms
            </span>
            {criticalRequest.size_formatted && (
              <span className="text-amber-700 dark:text-amber-300 text-xs">
                {criticalRequest.size_formatted}
              </span>
            )}
          </div>
        </div>
      )}

      {resources.map((resource, index) => {
        const stalledMs = resource.stalled_ms || 0;
        const ttfbMs = resource.ttfb_ms || 0;
        const downloadMs = (resource.total_ms || 0) - ttfbMs - stalledMs;

        return (
          <div key={index} className="group/row space-y-1">
            <div className="flex items-center gap-2 text-sm">
              <span className="font-mono text-xs truncate max-w-md">
                {resource.label || resource.full_url}
              </span>
              <span className="text-muted-foreground text-xs">
                {resource.total_ms}ms
              </span>
              <span className="text-muted-foreground text-xs">
                {resource.size_formatted}
              </span>
            </div>
            <div className="relative">
              <div className="flex items-center gap-1 h-6 bg-muted rounded overflow-hidden cursor-default">
                {stalledMs > 0 && (
                  <div
                    className="h-full bg-gray-400"
                    style={{ width: `${(stalledMs / maxTotalMs) * 100}%` }}
                  />
                )}
                {ttfbMs > 0 && (
                  <div
                    className="h-full bg-blue-500"
                    style={{ width: `${(ttfbMs / maxTotalMs) * 100}%` }}
                  />
                )}
                {downloadMs > 0 && (
                  <div
                    className="h-full bg-green-500"
                    style={{ width: `${(downloadMs / maxTotalMs) * 100}%` }}
                  />
                )}
              </div>
              {/* Tooltip — appears on hover over the entire bar */}
              <div className="pointer-events-none absolute bottom-full left-0 mb-1.5 hidden group-hover/row:flex items-center gap-3 rounded bg-gray-800 dark:bg-gray-700 px-3 py-1.5 text-[11px] text-white z-10 shadow-lg">
                {stalledMs > 0 && (
                  <span className="flex items-center gap-1.5">
                    <span className="inline-block w-2 h-2 rounded-sm bg-gray-400" />
                    Stalled {stalledMs}ms
                  </span>
                )}
                <span className="flex items-center gap-1.5">
                  <span className="inline-block w-2 h-2 rounded-sm bg-blue-500" />
                  TTFB {ttfbMs}ms
                </span>
                {downloadMs > 0 && (
                  <span className="flex items-center gap-1.5">
                    <span className="inline-block w-2 h-2 rounded-sm bg-green-500" />
                    Download {downloadMs}ms
                  </span>
                )}
              </div>
            </div>
          </div>
        );
      })}
      <div className="flex gap-6 pt-4 text-sm">
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 bg-gray-400 rounded" />
          <span>Stalled</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 bg-blue-500 rounded" />
          <span>TTFB</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 bg-green-500 rounded" />
          <span>Download</span>
        </div>
      </div>
    </div>
  );
}
