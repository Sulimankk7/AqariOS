/**
 * Theme helper utilities for AqariOS Design System.
 */

import { colors } from "./colors";

export type ThemeMode = "light" | "dark";

export function getSemanticColor(
  token: keyof typeof colors.light,
  mode: ThemeMode = "light"
): string {
  return colors[mode][token];
}

export function getCurrentThemeMode(): ThemeMode {
  if (typeof window === "undefined") return "light";
  return document.documentElement.classList.contains("dark") ? "dark" : "light";
}
