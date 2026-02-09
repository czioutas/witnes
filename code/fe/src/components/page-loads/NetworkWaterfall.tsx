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

  const maxTotalMs = Math.max(...resources.map((r) => r.total_ms || 0));

  return (
    <div className="space-y-2">
      {resources.map((resource) => (
        <div key={resource.id} className="space-y-1">
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
          <div className="flex items-center gap-1 h-6 bg-muted rounded overflow-hidden">
            {/* Stalled */}
            {(resource.stalled_ms || 0) > 0 && (
              <div
                className="h-full bg-gray-400"
                style={{
                  width: `${((resource.stalled_ms || 0) / maxTotalMs) * 100}%`,
                }}
                title={`Stalled: ${resource.stalled_ms || 0}ms`}
              />
            )}
            {/* TTFB */}
            {(resource.ttfb_ms || 0) > 0 && (
              <div
                className="h-full bg-blue-500"
                style={{
                  width: `${((resource.ttfb_ms || 0) / maxTotalMs) * 100}%`,
                }}
                title={`TTFB: ${resource.ttfb_ms || 0}ms`}
              />
            )}
            {/* Download */}
            {(resource.total_ms || 0) - (resource.ttfb_ms || 0) - (resource.stalled_ms || 0) > 0 && (
              <div
                className="h-full bg-green-500"
                style={{
                  width: `${(((resource.total_ms || 0) - (resource.ttfb_ms || 0) - (resource.stalled_ms || 0)) / maxTotalMs) * 100}%`,
                }}
                title={`Download: ${(resource.total_ms || 0) - (resource.ttfb_ms || 0) - (resource.stalled_ms || 0)}ms`}
              />
            )}
          </div>
        </div>
      ))}
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
