import { Button } from "../ui/button";
import { ArrowRight } from "lucide-react";

export default function HeroButtons() {
  return (
    <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
      <Button
        asChild
        size="lg"
        className="gap-2 bg-primary text-primary-foreground hover:bg-primary/90"
      >
        <a href="/free-month">
          Get 1 Month Free
          <ArrowRight className="h-4 w-4" />
        </a>
      </Button>
      <Button
        asChild
        size="lg"
        variant="outline"
        className="gap-2 border-border text-foreground hover:bg-secondary bg-transparent"
      >
        <a href="/authenticate/login">See it in action</a>
      </Button>
    </div>
  );
}
