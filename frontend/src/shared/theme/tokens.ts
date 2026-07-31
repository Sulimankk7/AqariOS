/**
 * AqariOS Design Token System — Programmatic TypeScript Token Architecture.
 *
 * Central single source of truth for Typography, Spacing, Radius, Elevation,
 * Borders, Animations, Focus Rings, Status Colors, and Chart Palette.
 */

import { brandColors, colors } from "./colors";

export const tokens = {
  brand: brandColors,
  colors,

  typography: {
    fontFamily: {
      sans: 'Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
      mono: 'JetBrains Mono, ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
    },
    fontSize: {
      xs: ["0.75rem", { lineHeight: "1rem" }],       // 12px
      sm: ["0.875rem", { lineHeight: "1.25rem" }],   // 14px
      base: ["1rem", { lineHeight: "1.5rem" }],      // 16px
      lg: ["1.125rem", { lineHeight: "1.75rem" }],   // 18px
      xl: ["1.25rem", { lineHeight: "1.75rem" }],    // 20px
      "2xl": ["1.5rem", { lineHeight: "2rem" }],     // 24px
      "3xl": ["1.875rem", { lineHeight: "2.25rem" }],// 30px
      "4xl": ["2.25rem", { lineHeight: "2.5rem" }],  // 36px
    },
    fontWeight: {
      normal: "400",
      medium: "500",
      semibold: "600",
      bold: "700",
    },
  },

  spacing: {
    0: "0px",
    1: "0.25rem",  // 4px
    2: "0.5rem",   // 8px
    3: "0.75rem",  // 12px
    4: "1rem",      // 16px
    5: "1.25rem",  // 20px
    6: "1.5rem",   // 24px
    8: "2rem",      // 32px
    10: "2.5rem",  // 40px
    12: "3rem",     // 48px
    16: "4rem",     // 64px
  },

  radius: {
    none: "0px",
    xs: "0.125rem", // 2px
    sm: "0.25rem",  // 4px
    md: "0.375rem", // 6px
    lg: "0.5rem",   // 8px (default --radius)
    xl: "0.75rem",  // 12px
    full: "9999px",
  },

  elevation: {
    none: "none",
    xs: "0 1px 2px 0 rgba(0, 0, 0, 0.05)",
    sm: "0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px -1px rgba(0, 0, 0, 0.1)",
    md: "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)",
    lg: "0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1)",
    xl: "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)",
    "2xl": "0 25px 50px -12px rgba(0, 0, 0, 0.25)",
  },

  borders: {
    default: "var(--border)",
    strong: "var(--border-strong)",
    muted: "var(--border-muted)",
    focus: "var(--border-focus)",
  },

  animations: {
    duration: {
      fast: "75ms",
      normal: "150ms",
      medium: "200ms",
      slow: "300ms",
    },
    easing: {
      easeInOut: "cubic-bezier(0.4, 0, 0.2, 1)",
      easeOut: "cubic-bezier(0, 0, 0.2, 1)",
      easeIn: "cubic-bezier(0.4, 0, 1, 1)",
    },
  },

  focusRings: {
    ring: "var(--ring)",
    offset: "2px",
    width: "2px",
    classes: "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
  },

  statusColors: {
    success: {
      text: "var(--success)",
      bg: "var(--success-bg)",
      foreground: "var(--success-foreground)",
    },
    warning: {
      text: "var(--warning)",
      bg: "var(--warning-bg)",
      foreground: "var(--warning-foreground)",
    },
    danger: {
      text: "var(--danger)",
      bg: "var(--danger-bg)",
      foreground: "var(--danger-foreground)",
    },
    info: {
      text: "var(--info)",
      bg: "var(--info-bg)",
      foreground: "var(--info-foreground)",
    },
  },

  chartPalette: {
    chart1: "var(--chart-1)",
    chart2: "var(--chart-2)",
    chart3: "var(--chart-3)",
    chart4: "var(--chart-4)",
    chart5: "var(--chart-5)",
  },
} as const;

export type DesignTokens = typeof tokens;
