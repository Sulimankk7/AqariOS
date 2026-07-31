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
      className={`text-[12.5px] font-medium flex items-center gap-1.5 mb-1.5 select-none transition-colors duration-350 ${
        isDark ? "text-gray-300" : "text-[#374151]"
      }`}
    >
      {children}
      {optional && (
        <span
          className={`text-[11px] font-normal transition-colors duration-350 ${
            isDark ? "text-gray-500" : "text-[#9CA3AF]"
          }`}
        >
          {optional}
        </span>
      )}
    </label>
  );
}

/** Standardized input element styling class string matching architectural glass spec */
export const inputClass =
  "w-full h-10 px-3.5 bg-white dark:bg-[#161B22]/80 text-[#111827] dark:text-gray-100 text-[13.5px] border border-[#E5E7EB] dark:border-white/10 rounded-lg outline-none transition-all duration-200 focus:border-[#A4AC86] dark:focus:border-[#656D4A] focus:ring-2 focus:ring-[#A4AC86]/20 dark:focus:ring-[#656D4A]/25 placeholder-[#C2C5AA] dark:placeholder-gray-500 shadow-2xs backdrop-blur-xs";
