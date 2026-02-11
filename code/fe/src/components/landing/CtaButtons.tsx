import { Button } from "../ui/button";
import { ArrowRight } from "lucide-react";

export default function CtaButtons() {
  return (
    <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
      <Button
        asChild
        size="lg"
        className="gap-2 bg-primary text-primary-foreground hover:bg-primary/90"
      >
        <a href="/authenticate/register">
          Start your 7-day free trial
          <ArrowRight className="h-4 w-4" />
        </a>
      </Button>
      <Button
        asChild
        size="lg"
        variant="outline"
        className="gap-2 border-border text-foreground hover:bg-secondary bg-transparent"
      >
        <a href="mailto:sales@witnes.io">Talk to us</a>
      </Button>
    </div>
  );
}
