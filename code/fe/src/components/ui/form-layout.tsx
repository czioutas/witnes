import type { ReactNode } from "react";
import { Button } from "./button";
import { ArrowLeft } from "lucide-react";

interface FormLayoutProps {
  /** Optional back button configuration */
  backButton?: {
    label: string;
    /** Either a URL to navigate to, or a callback function */
    href?: string;
    onClick?: () => void;
  };
  /** Content to render inside the form card */
  children: ReactNode;
  /** Optional custom max width class (default: max-w-4xl) */
  maxWidth?: string;
}

/**
 * Consistent form layout component for dashboard forms
 *
 * Features:
 * - Optional back button (supports both href and onClick)
 * - Centered container with max-width
 * - Bordered card with proper spacing
 * - Consistent styling across all forms
 */
export function FormLayout({
  backButton,
  children,
  maxWidth = "max-w-4xl",
}: FormLayoutProps) {
  const handleBackClick = () => {
    if (backButton?.onClick) {
      backButton.onClick();
    } else if (backButton?.href) {
      window.location.href = backButton.href;
    }
  };

  return (
    <div className={`mx-auto w-full ${maxWidth} space-y-6 py-4`}>
      {/* Back Button */}
      {backButton && (
        <Button
          variant="ghost"
          onClick={handleBackClick}
          className="w-fit px-0 text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          {backButton.label}
        </Button>
      )}

      {/* Form Card */}
      <div className="rounded-lg border bg-card p-6">{children}</div>
    </div>
  );
}
