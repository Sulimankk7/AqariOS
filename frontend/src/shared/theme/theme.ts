/**
 * Theme helper utilities for AqariOS Design System.
 */

import { colors } from "./colors";

export type ColorThemeMode = "light" | "dark";

export function getSemanticColor(
  token: keyof typeof colors.light,
  mode: ColorThemeMode = "light"
): string {
  return colors[mode][token];
}

export function getCurrentThemeMode(): ColorThemeMode {
  if (typeof window === "undefined") return "light";
  return document.documentElement.classList.contains("dark") ? "dark" : "light";
}
