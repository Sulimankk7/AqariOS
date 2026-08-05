/**
 * General-purpose utility functions.
 * Keep this file lean — only place utilities here that are used
 * across two or more features. Feature-specific helpers live in
 * the feature's own utils/ folder.
 */

/**
 * Delay execution for `ms` milliseconds.
 * Useful for simulating async operations during development.
 */
export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Clamp a number between min and max (inclusive).
 */
export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/**
 * Returns `true` if the string is a valid email address (basic RFC check).
 */
export function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

/**
 * Strip leading/trailing whitespace and collapse internal whitespace.
 */
export function normaliseString(value: string): string {
  return value.trim().replace(/\s+/g, " ");
}

/**
 * Format a phone number string for display (removes non-digit chars).
 */
export function normalisePhone(value: string): string {
  return value.replace(/[^\d+]/g, "");
}

export * from './errorHandling';

