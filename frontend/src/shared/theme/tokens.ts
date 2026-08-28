import { brandColors, colors } from "./colors";

export const tokens = {
  brand: brandColors,
  colors,
  typography: {
    fontFamily: {
      arabic: 'Tajawal, Arial, sans-serif',
      english: '"Roboto Flex", Roboto, Arial, sans-serif',
      mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
    },
    scale: {
      display: { large: [57, 64, 400], medium: [45, 52, 400], small: [36, 44, 400] },
      headline: { large: [32, 40, 400], medium: [28, 36, 400], small: [24, 32, 400] },
      title: { large: [22, 28, 400], medium: [16, 24, 500], small: [14, 20, 500] },
      body: { large: [16, 24, 400], medium: [14, 20, 400], small: [12, 16, 400] },
      label: { large: [14, 20, 500], medium: [12, 16, 500], small: [11, 16, 500] },
    },
  },
  spacing: { 0: "0", 1: "0.25rem", 2: "0.5rem", 3: "0.75rem", 4: "1rem", 5: "1.25rem", 6: "1.5rem", 8: "2rem", 10: "2.5rem", 12: "3rem", 16: "4rem" },
  shape: { xs: "4px", sm: "8px", md: "16px", lg: "28px", full: "9999px" },
  elevation: {
    e0: "none",
    e1: "0 1px 2px rgb(51 61 41 / 0.08), 0 1px 3px 1px rgb(51 61 41 / 0.05)",
    e2: "0 1px 2px rgb(51 61 41 / 0.08), 0 2px 6px 2px rgb(51 61 41 / 0.06)",
    e3: "0 1px 3px rgb(51 61 41 / 0.08), 0 4px 8px 3px rgb(51 61 41 / 0.07)",
    e4: "0 2px 3px rgb(51 61 41 / 0.08), 0 6px 10px 4px rgb(51 61 41 / 0.07)",
  },
  motion: { fast: "75ms", normal: "150ms", medium: "200ms", slow: "300ms" },
  focusRing: "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
} as const;

export type DesignTokens = typeof tokens;
