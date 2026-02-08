import { Button } from "../ui/button";

interface PricingButtonsProps {
  planName: string;
}

export default function PricingButtons({ planName }: PricingButtonsProps) {
  return (
    <Button
      size="lg"
      className="w-full bg-primary text-primary-foreground hover:bg-primary/90"
    >
      <a href="/authenticate/register">Start 7-day free trial</a>
    </Button>
  );
}
