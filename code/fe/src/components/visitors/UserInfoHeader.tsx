import { User, Monitor, Smartphone } from "lucide-react";

interface UserInfoHeaderProps {
  userId: string;
  totalPageLoads?: number;
  browsers?: string[];
  operatingSystems?: string[];
}

export function UserInfoHeader({
  userId,
  totalPageLoads,
  browsers,
  operatingSystems,
}: UserInfoHeaderProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
          <User className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{userId}</h1>
          <p className="text-sm text-muted-foreground">
            {totalPageLoads !== undefined ? `${totalPageLoads} total page loads` : "Loading..."}
          </p>
        </div>
      </div>

      <div className="flex gap-6 pt-2">
        {browsers && browsers.length > 0 && (
          <div className="flex items-center gap-2">
            <Monitor className="h-4 w-4 text-muted-foreground" />
            <div>
              <p className="text-sm font-medium">Browsers</p>
              <p className="text-sm text-muted-foreground">
                {browsers.join(", ") || "Not available"}
              </p>
            </div>
          </div>
        )}

        {operatingSystems && operatingSystems.length > 0 && (
          <div className="flex items-center gap-2">
            <Smartphone className="h-4 w-4 text-muted-foreground" />
            <div>
              <p className="text-sm font-medium">Operating Systems</p>
              <p className="text-sm text-muted-foreground">
                {operatingSystems.join(", ") || "Not available"}
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
