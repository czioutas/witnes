"use client";

import { useState } from "react";
import { Menu, X } from "lucide-react";
import { AuthActions } from "../AuthActions";

export default function NavbarClient() {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <>
      {/* - Mobile: py-2 (8px) for a tight vertical profile, 3-column grid for centering.
        - Desktop: py-4 (16px), switches to flex-between.
      */}
      <nav className="mx-auto grid max-w-6xl grid-cols-3 items-center px-6 py-2 md:py-4 md:flex md:justify-between">
        {/* 1. Left Spacer (Takes up 1/3 of the width on mobile to offset the toggle) */}
        <div className="flex md:hidden" />

        {/* 2. Logo - Larger on mobile (h-14), standard on desktop (h-12) */}
        <a
          href="/"
          className="flex items-center justify-center md:justify-start"
        >
          <img
            src="/logo/witnes-dark.svg"
            alt="Witnes"
            className="h-14 w-auto hidden dark:block md:h-12"
          />
          <img
            src="/logo/witnes-light.svg"
            alt="Witnes"
            className="h-14 w-auto dark:hidden md:h-12"
          />
        </a>

        {/* 3. Desktop Navigation Links (Hidden on mobile) */}
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

        {/* 4. Right Side Actions (Auth on desktop, Toggle on mobile) */}
        <div className="flex items-center justify-end">
          <div className="hidden md:block">
            <AuthActions />
          </div>

          <button
            type="button"
            className="flex items-center justify-end text-foreground md:hidden"
            onClick={() => setMobileOpen(!mobileOpen)}
            aria-label="Toggle menu"
          >
            {mobileOpen ? (
              <X className="h-6 w-6" />
            ) : (
              <Menu className="h-6 w-6" />
            )}
          </button>
        </div>
      </nav>

      {/* Mobile Menu Dropdown */}
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
