/**
 * Navigation Components — Tabs & Pagination toolbars.
 */

import React from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface TabItem {
  id: string;
  label: string;
  count?: number;
}

export interface TabsProps {
  items: TabItem[];
  activeId: string;
  onChange: (id: string) => void;
  className?: string;
}

export function Tabs({ items, activeId, onChange, className = "" }: TabsProps) {
  return (
    <div className={`flex items-center gap-1 border-b border-border overflow-x-auto ${className}`}>
      {items.map((tab) => {
        const isActive = tab.id === activeId;
        return (
          <button
            key={tab.id}
            onClick={() => onChange(tab.id)}
            className={`flex items-center gap-2 px-3.5 py-2 text-xs font-semibold border-b-2 transition-all cursor-pointer whitespace-nowrap ${
              isActive
                ? "border-primary text-primary"
                : "border-transparent text-muted-foreground hover:text-foreground hover:border-border-strong"
            }`}
          >
            <span>{tab.label}</span>
            {tab.count !== undefined && (
              <span
                className={`px-1.5 py-0.5 rounded-full text-[10px] font-mono ${
                  isActive
                    ? "bg-primary/10 text-primary"
                    : "bg-secondary text-muted-foreground"
                }`}
              >
                {tab.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  totalRecords?: number;
  pageSize?: number;
  className?: string;
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  totalRecords,
  pageSize = 10,
  className = "",
}: PaginationProps) {
  const { t } = useTranslation();

  if (totalPages <= 1) return null;

  const from = (currentPage - 1) * pageSize + 1;
  const to = totalRecords ? Math.min(currentPage * pageSize, totalRecords) : currentPage * pageSize;

  return (
    <div className={`flex flex-col sm:flex-row items-center justify-between gap-3 pt-3 text-xs text-muted-foreground ${className}`}>
      {totalRecords !== undefined && (
        <span>
          {t("table.showingResults", { from, to, total: totalRecords })}
        </span>
      )}

      <div className="flex items-center gap-1">
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          aria-label={t("table.previous")}
          className="flex items-center gap-1 px-2.5 py-1 rounded-md border border-border bg-card hover:bg-secondary text-foreground disabled:opacity-50 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <ChevronLeft className="w-3.5 h-3.5 rtl:rotate-180" />
          <span>{t("table.previous")}</span>
        </button>

        <span className="px-2 font-mono text-foreground font-semibold">
          {currentPage} / {totalPages}
        </span>

        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          aria-label={t("table.next")}
          className="flex items-center gap-1 px-2.5 py-1 rounded-md border border-border bg-card hover:bg-secondary text-foreground disabled:opacity-50 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <span>{t("table.next")}</span>
          <ChevronRight className="w-3.5 h-3.5 rtl:rotate-180" />
        </button>
      </div>
    </div>
  );
}
