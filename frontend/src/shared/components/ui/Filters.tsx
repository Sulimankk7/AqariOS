/**
 * SearchBar & Filters Components — Enterprise filter toolbars.
 * Provides search input with instant clear button and filter select pills.
 */

import React from "react";
import { Search, X, Filter } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface SearchBarProps {
  value: string;
  onChange: (val: string) => void;
  placeholder?: string;
  className?: string;
}

export function SearchBar({
  value,
  onChange,
  placeholder,
  className = "",
}: SearchBarProps) {
  const { t } = useTranslation();

  return (
    <div className={`relative flex items-center w-full max-w-sm ${className}`}>
      <Search className="w-4 h-4 absolute start-3 text-muted-foreground pointer-events-none" />
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder || t("common.searchPlaceholder")}
        className="w-full ps-9 pe-8 py-1.5 rounded-lg border border-border bg-card text-foreground text-xs placeholder:text-muted-foreground focus:outline-none focus:border-border-strong focus:ring-1 focus:ring-ring transition-colors"
      />
      {value && (
        <button
          onClick={() => onChange("")}
          aria-label={t("common.clear")}
          className="absolute end-2 p-1 text-muted-foreground hover:text-foreground cursor-pointer"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      )}
    </div>
  );
}

export interface FilterOption {
  label: string;
  value: string;
}

export interface FiltersProps {
  options: FilterOption[];
  selected: string;
  onChange: (value: string) => void;
  label?: string;
  className?: string;
}

export function Filters({
  options,
  selected,
  onChange,
  label,
  className = "",
}: FiltersProps) {
  const { t } = useTranslation();

  return (
    <div className={`flex items-center gap-2 flex-wrap ${className}`}>
      {label && (
        <span className="text-xs font-semibold text-muted-foreground flex items-center gap-1">
          <Filter className="w-3.5 h-3.5" />
          <span>{label}:</span>
        </span>
      )}
      <div className="flex items-center gap-1.5 flex-wrap">
        <button
          onClick={() => onChange("all")}
          className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors cursor-pointer ${
            selected === "all"
              ? "bg-primary text-primary-foreground border-primary"
              : "bg-card text-muted-foreground border-border hover:bg-secondary"
          }`}
        >
          {t("common.filter")} ({t("common.clear")})
        </button>
        {options.map((opt) => (
          <button
            key={opt.value}
            onClick={() => onChange(opt.value)}
            className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors cursor-pointer ${
              selected === opt.value
                ? "bg-primary text-primary-foreground border-primary"
                : "bg-card text-muted-foreground border-border hover:bg-secondary"
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>
    </div>
  );
}
