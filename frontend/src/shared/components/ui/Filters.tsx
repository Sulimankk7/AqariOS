/**
 * SearchBar & Filters Components — Enterprise filter toolbars.
 * Provides search input with instant clear button and filter select pills.
 */

import React from "react";
import { Filter } from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { SearchField } from "@/shared/ui/search-field";

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
    <SearchField
        className={`w-full max-w-sm ${className}`}
        value={value}
        onValueChange={onChange}
        placeholder={placeholder || t("common.searchPlaceholder")}
        clearLabel={t("common.clear")}
    />
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
