export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="border-t w-full">
      <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8 py-10">
        <div className="flex flex-col items-center text-center gap-6">
          <img src="/logo/witnes-dark.svg" alt="Witnes" className="h-8" />

          <div className="flex flex-wrap justify-center gap-6 text-sm">
            <a
              href="/#why"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Why we exist
            </a>
            <a
              href="/#impact"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Impact
            </a>
            <a
              href="/#reports"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Reports
            </a>
            <a
              href="/#principles"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Principles
            </a>
          </div>

          <div className="flex flex-wrap justify-center gap-6 text-sm">
            <a
              href="/solutions/emissions-per-product"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Emissions per Product
            </a>
            <a
              href="/solutions/compliance-audit"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Compliance & Audit
            </a>
            <a
              href="/solutions/cbam-reporting"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              CBAM Reporting
            </a>
            <a
              href="/solutions/guided-co2-measurement"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Guided CO₂ Measurement
            </a>
          </div>

          <div className="flex flex-wrap justify-center gap-6 text-sm">
            <a
              href="/methodology"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Methodology
            </a>
            <a
              href="/terms"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Terms
            </a>
            <a
              href="/privacy"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Privacy
            </a>
            <a
              href="/imprint"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Imprint
            </a>
            <a
              href="mailto:hello@witnes.io"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Contact
            </a>
          </div>

          <p className="text-sm text-muted-foreground">
            &copy; {currentYear} Green Actions. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}
