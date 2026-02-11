import {
  Accordion,
  AccordionItem,
  AccordionTrigger,
  AccordionContent,
} from "../ui/accordion";

const supportFaqs = [
  {
    id: "support-1",
    question:
      'How does Witnes help when a user says "the site is slow"?',
    answer:
      'It replaces the "guessing game" with a Session Receipt. Instead of asking the user for screenshots or their internet speed, you simply look up their ID in Witnes. You\'ll see an up to 30-day chronological history that proves if the lag was caused by their local Wi-Fi, a slow response from your server (API), or a frontend render issue.',
  },
  {
    id: "support-2",
    question: "Do I need to be a developer to understand the dashboard?",
    answer:
      'No. While we capture deep technical data, we translate it for your Support team. We clearly label bottlenecks as "Slow Wi-Fi," "API Latency," or "Frontend Render" so anyone can identify the root cause of a ticket in seconds without escalating to engineering.',
  },
  {
    id: "support-3",
    question: "How is this different from session replay tools like Hotjar?",
    answer:
      "Most tools show you what the user did (video); Witnes shows you why the site failed them (data). We are a lightweight diagnostic tool focused on the technical evidence — network waterfalls, API timings, and device health — rather than just a video of a mouse moving.",
  },
];

const techFaqs = [
  {
    id: "tech-1",
    question: "What is the actual performance impact of the tracker?",
    answer:
      "Negligible. The w.min.js script is under 5kb and loads asynchronously. We use the PerformanceObserver API for passive monitoring and navigator.sendBeacon to transmit data. This ensures we capture every detail without ever blocking the main thread or impacting your Core Web Vitals.",
  },
  {
    id: "tech-2",
    question:
      "How do you distinguish between Network, API, and Frontend lag?",
    answer:
      "We pull raw data from the Resource Timing API for every interaction. By calculating the delta between requestStart and responseStart, we isolate TTFB (Server processing). We then compare this to the user's effective bandwidth and RTT to determine if the bottleneck is the user's connection or your infrastructure.",
  },
  {
    id: "tech-3",
    question:
      "Does Witnes support Single Page Applications (React, Vue, Next.js)?",
    answer:
      'Yes, automatically. We intercept the browser\'s History API (pushState and replaceState). When a user navigates between "pages" in an SPA, Witnes detects the route change and captures a new performance snapshot after a brief "settle" period, ensuring soft-navigations are tracked just as accurately as full page loads.',
  },
];

export default function FaqAccordion() {
  return (
    <div className="mt-16 grid gap-12 md:grid-cols-2">
      <div>
        <p className="mb-6 text-sm font-semibold uppercase tracking-widest text-muted-foreground">
          For Support & CS Teams
        </p>
        <Accordion type="single" collapsible>
          {supportFaqs.map((faq) => (
            <AccordionItem key={faq.id} value={faq.id}>
              <AccordionTrigger className="text-left text-base font-semibold text-foreground">
                {faq.question}
              </AccordionTrigger>
              <AccordionContent className="text-muted-foreground leading-relaxed">
                {faq.answer}
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>
      </div>

      <div>
        <p className="mb-6 text-sm font-semibold uppercase tracking-widest text-muted-foreground">
          For Developers
        </p>
        <Accordion type="single" collapsible>
          {techFaqs.map((faq) => (
            <AccordionItem key={faq.id} value={faq.id}>
              <AccordionTrigger className="text-left text-base font-semibold text-foreground">
                {faq.question}
              </AccordionTrigger>
              <AccordionContent className="text-muted-foreground leading-relaxed">
                {faq.answer}
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>
      </div>
    </div>
  );
}
