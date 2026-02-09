import { VisitorsTable } from '../visitors/VisitorsTable';

export function VisitorsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Visitors</h1>
        <p className="text-muted-foreground mt-2">
          View and analyze your monitored visitors' activity
        </p>
      </div>

      <VisitorsTable />
    </div>
  );
}
