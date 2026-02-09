import { AlertTriangle, CheckCircle } from "lucide-react";

interface JankReportsProps {
  jankReports: string[];
}

export function JankReports({ jankReports }: JankReportsProps) {
  if (!jankReports || jankReports.length === 0) {
    return (
      <div className="flex items-center gap-3 p-6 bg-green-50 dark:bg-green-950/20 border border-green-200 dark:border-green-900 rounded-lg">
        <CheckCircle className="h-6 w-6 text-green-600 dark:text-green-400" />
        <div>
          <h3 className="font-semibold text-green-900 dark:text-green-100">
            No Performance Issues Detected
          </h3>
          <p className="text-sm text-green-700 dark:text-green-300">
            This page load had smooth performance with no jank detected.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 mb-4">
        <AlertTriangle className="h-5 w-5 text-yellow-600" />
        <h3 className="font-semibold">
          {jankReports.length} Performance Issue{jankReports.length !== 1 ? "s" : ""} Detected
        </h3>
      </div>
      {jankReports.map((report, index) => (
        <div
          key={index}
          className="p-4 bg-yellow-50 dark:bg-yellow-950/20 border border-yellow-200 dark:border-yellow-900 rounded-lg"
        >
          <p className="text-sm text-yellow-900 dark:text-yellow-100">{report}</p>
        </div>
      ))}
    </div>
  );
}
