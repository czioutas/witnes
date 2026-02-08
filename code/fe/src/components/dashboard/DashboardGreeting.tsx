import { useAuth } from "@/contexts/AuthContext";

export function DashboardGreeting() {
  const { user } = useAuth();
  const currentDate = new Date();

  const formatGreeting = () => {
    const hour = currentDate.getHours();
    const firstName = user?.firstName || "";

    // Array of varied greeting messages inspired by Claude Code
    const morningGreetings = [
      "Good morning",
      "Rise and shine",
      "Top of the morning",
      "Morning",
      "Early bird gets the worm",
      "Fresh start ahead",
      "New day, new emissions to track",
    ];

    const afternoonGreetings = [
      "Good afternoon",
      "Afternoon",
      "Hope your day's going well",
      "Getting things done",
      "Midday check-in",
      "Keeping it green",
      "Halfway there",
    ];

    const eveningGreetings = [
      "Good evening",
      "Evening",
      "Hope you had a productive day",
      "Winding down",
      "End of day recap",
      "Time to review your impact",
      "Evening emissions check",
      "Another day, another step towards sustainability",
    ];

    // Select greeting array based on time of day
    let greetings: string[];
    if (hour < 12) {
      greetings = morningGreetings;
    } else if (hour < 18) {
      greetings = afternoonGreetings;
    } else {
      greetings = eveningGreetings;
    }

    // Pick a greeting from the appropriate array
    // Use date as seed to keep it consistent throughout the session
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    const day = currentDate.getDate();

    // Create a simple hash from the date components
    const seed =
      (year * 10000 + month * 100 + day * 17 + hour * 3) % greetings.length;
    const greeting = greetings[seed];

    return firstName ? `${greeting}, ${firstName}` : greeting;
  };

  const formatDate = () => {
    return currentDate.toLocaleDateString("en-US", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  return (
    <div className="mb-3">
      <h1 className="text-2xl font-bold">{formatGreeting()}</h1>
      <p className="text-sm text-muted-foreground">{formatDate()}</p>
    </div>
  );
}
