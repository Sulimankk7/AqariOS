/**
 * AqariOS Brand Color Primitives & Semantic Color Palette.
 * Mandated Brand Colors for Property Management System.
 */

export const brandColors = {
  browns: {
    900: "#582F0E",
    700: "#7F4F24",
    600: "#936639",
    400: "#A68A64",
    200: "#B6AD90",
  },
  greens: {
    900: "#333D29",
    800: "#414833",
    600: "#656D4A",
    400: "#A4AC86",
    200: "#C2C5AA",
  },
} as const;

export const colors = {
  light: {
    background: "#FAFAF7",
    surface: "#FFFFFF",
    card: "#FFFFFF",
    border: "rgba(0, 0, 0, 0.08)",
    borderStrong: "rgba(0, 0, 0, 0.16)",
    textPrimary: "#1A1D20",
    textSecondary: "#656D4A",
    textMuted: "#A3A899",
    primary: "#414833",
    primaryBg: "#F3F4F2",
    secondary: "#656D4A",
    secondaryBg: "#F3F4F2",
    accent: "#A4AC86",
    success: "#059669",
    successBg: "#ECFDF5",
    warning: "#D97706",
    warningBg: "#FFFBEB",
    danger: "#DC2626",
    dangerBg: "#FEF2F2",
    info: "#2563EB",
    infoBg: "#EFF6FF",
  },
  dark: {
    background: "#0E1116",
    surface: "#161B22",
    card: "#161B22",
    border: "rgba(255, 255, 255, 0.08)",
    borderStrong: "rgba(255, 255, 255, 0.16)",
    textPrimary: "#F0F3F6",
    textSecondary: "#9CA3AF",
    textMuted: "#6B7280",
    primary: "#414833",
    primaryBg: "#1F2937",
    secondary: "#9CA3AF",
    secondaryBg: "rgba(255, 255, 255, 0.06)",
    accent: "#656D4A",
    success: "#10B981",
    successBg: "rgba(6, 78, 59, 0.4)",
    warning: "#F59E0B",
    warningBg: "rgba(120, 53, 15, 0.4)",
    danger: "#EF4444",
    dangerBg: "rgba(127, 29, 29, 0.4)",
    info: "#3B82F6",
    infoBg: "rgba(30, 58, 138, 0.4)",
  },
} as const;
