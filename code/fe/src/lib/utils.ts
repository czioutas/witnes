import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * Converts a snake_case string to Title Case
 * @param str The snake_case string to convert
 * @returns The string in Title Case with spaces
 * @example
 * toTitleCase("purchase_goods") // "Purchase Goods"
 * toTitleCase("variant") // "Variant"
 */
export function toTitleCase(str: string): string {
  return str
    .replace(/_/g, " ")
    .replace(/\b\w/g, (l) => l.toUpperCase());
}

/**
 * Converts PascalCase or snake_case to human-readable format with spaces
 * @param str The string to convert (PascalCase, camelCase, or snake_case)
 * @returns The string with spaces and proper capitalization
 * @example
 * toReadableText("TravelMode") // "Travel Mode"
 * toReadableText("manufactured_product") // "Manufactured Product"
 * toReadableText("businessTravel") // "Business Travel"
 */
export function toReadableText(str: string): string {
  return str
    // First handle snake_case
    .replace(/_/g, " ")
    // Then add spaces before capital letters (for PascalCase/camelCase)
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    // Capitalize first letter of each word
    .replace(/\b\w/g, (l) => l.toUpperCase());
}

/**
 * Gets the base URL for the application
 * @returns The base URL from environment variables or default
 */
export function getBaseUrl(): string {
  return (
    (typeof import.meta !== 'undefined' && import.meta.env?.PUBLIC_API_BASE_URL) ||
    process.env.PUBLIC_API_BASE_URL ||
    'http://localhost:5000/api'
  );
}