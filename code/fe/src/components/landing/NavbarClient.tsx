"use client";

import { useState } from "react";
import { Menu, X } from "lucide-react";
import { AuthActions } from "../AuthActions";

export default function NavbarClient() {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <>
      <nav className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <a href="/" className="flex items-center gap-2">
          <img
            src="/logo/witnes-dark.svg"
            alt="Witnes"
            className="h-12 w-auto hidden dark:block"
          />
          <img
            src="/logo/witnes-light.svg"
            alt="Witnes"
            className="h-12 w-auto dark:hidden"
          />
        </a>

        <div className="hidden items-center gap-8 md:flex">
          <a
            href="/#evidence"
            className="text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            Evidence
          </a>
          <a
            href="/#pillars"
            className="text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            How We Track
          </a>
          <a
            href="/#integration"
            className="text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            Integration
          </a>
          <a
            href="/#privacy"
            className="text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            Privacy
          </a>
          <a
            href="/#pricing"
            className="text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            Pricing
          </a>
        </div>

        <div className="hidden md:block">
          <AuthActions />
        </div>

        <button
          type="button"
          className="text-foreground md:hidden"
          onClick={() => setMobileOpen(!mobileOpen)}
          aria-label="Toggle menu"
        >
          {mobileOpen ? (
            <X className="h-5 w-5" />
          ) : (
            <Menu className="h-5 w-5" />
          )}
        </button>
      </nav>

      {mobileOpen && (
        <div className="border-t border-border/50 bg-background/95 px-6 pb-6 pt-4 backdrop-blur-xl md:hidden">
          <div className="flex flex-col gap-4">
            <a
              href="/#evidence"
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
              onClick={() => setMobileOpen(false)}
            >
              Evidence
            </a>
            <a
              href="/#pillars"
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
              onClick={() => setMobileOpen(false)}
            >
              How We Track
            </a>
            <a
              href="/#integration"
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
              onClick={() => setMobileOpen(false)}
            >
              Integration
            </a>
            <a
              href="/#privacy"
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
              onClick={() => setMobileOpen(false)}
            >
              Privacy
            </a>
            <a
              href="/#pricing"
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
              onClick={() => setMobileOpen(false)}
            >
              Pricing
            </a>
            <div className="mt-2">
              <AuthActions />
            </div>
          </div>
        </div>
      )}
    </>
  );
}
