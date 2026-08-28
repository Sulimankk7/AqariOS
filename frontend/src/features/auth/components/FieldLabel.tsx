/**
 * FieldLabel — Standardized form label component with optional indicator.
 * Standardized input styling classes matching Architectural Glass Skyscraper design system.
 */

import React from "react";

interface FieldLabelProps {
  children: React.ReactNode;
  optional?: string;
  isDark?: boolean;
}

export function FieldLabel({ children, optional, isDark = false }: FieldLabelProps) {
  return (
    <label
      className="text-[13px] font-semibold flex items-center gap-1.5 mb-1.5 text-on-surface-variant select-none transition-colors duration-350"
    >
      {children}
      {optional && (
        <span
          className="text-[11px] font-medium text-muted-foreground transition-colors duration-350"
        >
          {optional}
        </span>
      )}
    </label>
  );
}

/** Standardized input element styling class string matching architectural glass spec */
export const inputClass =
  "w-full h-10 px-3.5 bg-input-background text-foreground text-[13.5px] border border-outline rounded-lg outline-none transition-all duration-200 focus:border-primary focus:ring-2 focus:ring-primary/20 placeholder:text-muted-foreground disabled:bg-disabled-container disabled:text-disabled-foreground";
