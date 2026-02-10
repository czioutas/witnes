import { Button } from "../ui/button";

export default function IntegrationButton() {
  return (
    <Button
      asChild
      size="lg"
      className="gap-2 bg-primary text-primary-foreground hover:bg-primary/90 font-semibold"
    >
      <a href="/integration">View Integration Guide</a>
    </Button>
  );
}
