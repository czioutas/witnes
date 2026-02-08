import { useMemo } from "react";

interface EmissionValueProps {
  /**
   * Emission value in kg CO2e
   */
  value: number | null | undefined;

  /**
   * Number of decimal places to show (default: 4)
   */
  precision?: number;

  /**
   * Threshold in kg to convert to tonnes (default: 1000)
   */
  tonneThreshold?: number;

  /**
   * Show the unit suffix (default: true)
   */
  showUnit?: boolean;

  /**
   * If true, shows raw value without automatic unit conversion or rounding (default: false)
   */
  raw?: boolean;

  /**
   * Custom className for styling
   */
  className?: string;
}

/**
 * Formats and displays emission values with appropriate units and localization.
 * Automatically converts to tonnes when value is large enough.
 * Uses browser locale for number formatting.
 * Displays values in monospace font for better alignment.
 */
export function EmissionValue({
  value,
  precision = 4,
  tonneThreshold = 1000,
  showUnit = true,
  raw = false,
  className,
}: EmissionValueProps) {
  const formatted = useMemo(() => {
    if (value === null || value === undefined || isNaN(value)) {
      return { number: "—", unit: "" };
    }

    // Raw mode: show exact value with specified precision, no unit conversion
    if (raw) {
      const formattedNumber = value.toFixed(precision);
      return { number: formattedNumber, unit: "kg CO₂e" };
    }

    // Convert to grams for values less than 1 kg
    if (Math.abs(value) < 1) {
      const displayValue = value * 1000; // Convert kg to g
      const formattedNumber = displayValue.toLocaleString(undefined, {
        minimumFractionDigits: 0,
        maximumFractionDigits: precision,
      });
      return { number: formattedNumber, unit: "g CO₂e" };
    }

    // Determine if we should display in tonnes
    const shouldUseTonnes = Math.abs(value) >= tonneThreshold;
    const displayValue = shouldUseTonnes ? value / 1000 : value;
    const unit = shouldUseTonnes ? "t CO₂e" : "kg CO₂e";

    // Format number with browser locale - use 2 decimals for kg values
    const kgPrecision = shouldUseTonnes ? precision : 2;
    const formattedNumber = displayValue.toLocaleString(undefined, {
      minimumFractionDigits: 0,
      maximumFractionDigits: kgPrecision,
    });

    return { number: formattedNumber, unit };
  }, [value, precision, tonneThreshold, raw]);

  return (
    <span className={`font-mono font-medium text-foreground ${className || ""}`}>
      {formatted.number}
      {showUnit && formatted.unit && formatted.unit}
    </span>
  );
}
