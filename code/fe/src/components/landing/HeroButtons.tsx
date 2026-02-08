import { Button } from "../ui/button";
import { ArrowRight } from "lucide-react";

export default function HeroButtons() {
  return (
    <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
      <Button
        size="lg"
        className="gap-2 bg-primary text-primary-foreground hover:bg-primary/90"
      >
        <a href="/authenticate/register">Start 7-day free trial</a>
        <ArrowRight className="h-4 w-4" />
      </Button>
      <Button
        size="lg"
        variant="outline"
        className="gap-2 border-border text-foreground hover:bg-secondary bg-transparent"
      >
        <a href="/authenticate/login">See it in action</a>
      </Button>
    </div>
  );
}
