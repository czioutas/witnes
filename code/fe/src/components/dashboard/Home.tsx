import { useState, useEffect } from "react";
import { DashboardGreeting } from "@/components/dashboard/DashboardGreeting";
import { getWitnesServerAPI } from "@/generated/api";

export function Home() {
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    const api = getWitnesServerAPI();

    // Fetch recent activities
  };

  return (
    <div className="container mx-auto">
      {/* Greeting and Date */}
      <DashboardGreeting />

      {/* Getting Started Tasks */}
      <div className="mb-5"></div>
    </div>
  );
}
