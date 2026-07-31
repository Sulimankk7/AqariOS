/**
 * StatusBadge Component — Enterprise status indicator pill.
 * Supports active, pending, danger/overdue, success/paid, warning, info, and neutral variants.
 */

import React from "react";

export type StatusVariant =
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "neutral"
  | "brand";

export interface StatusBadgeProps {
  label: string;
  variant?: StatusVariant;
  showDot?: boolean;
  size?: "sm" | "md";
  className?: string;
}

const variantStyles: Record<StatusVariant, { bg: string; text: string; dot: string }> = {
  success: {
    bg: "bg-success-bg border-success/30",
    text: "text-success",
    dot: "bg-success",
  },
  warning: {
    bg: "bg-warning-bg border-warning/30",
    text: "text-warning",
    dot: "bg-warning",
  },
  danger: {
    bg: "bg-danger-bg border-danger/30",
    text: "text-danger",
    dot: "bg-danger",
  },
  info: {
    bg: "bg-info-bg border-info/30",
    text: "text-info",
    dot: "bg-info",
  },
  neutral: {
    bg: "bg-secondary border-border",
    text: "text-muted-foreground",
    dot: "bg-muted-foreground",
  },
  brand: {
    bg: "bg-brand-green-200/50 dark:bg-brand-green-900/40 border-brand-green-600/30",
    text: "text-brand-green-900 dark:text-brand-green-200",
    dot: "bg-brand-green-600",
  },
};

export function StatusBadge({
  label,
  variant = "neutral",
  showDot = true,
  size = "md",
  className = "",
}: StatusBadgeProps) {
  const styles = variantStyles[variant];

  const sizeClasses = size === "sm" ? "px-2 py-0.5 text-[10px]" : "px-2.5 py-1 text-xs";

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border font-medium transition-colors ${styles.bg} ${styles.text} ${sizeClasses} ${className}`}
    >
      {showDot && (
        <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${styles.dot}`} aria-hidden="true" />
      )}
      <span className="truncate">{label}</span>
    </span>
  );
}
