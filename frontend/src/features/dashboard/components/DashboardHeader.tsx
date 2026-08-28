/**
 * DashboardHeader Component — Enterprise Header for AqariOS Dashboard.
 * Uses namespaced translations and formatters from useTranslation().
 */

import React from "react";
import { Calendar, RefreshCw } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface DashboardHeaderProps {
  onRefresh?: () => void;
  isRefetching?: boolean;
  dataUpdatedAt?: number;
}

export function DashboardHeader({
  onRefresh,
  isRefetching,
  dataUpdatedAt,
}: DashboardHeaderProps) {
  const { t, formatDate, formatTime } = useTranslation();

  const formattedDate = formatDate(new Date(), {
    weekday: "long",
    year: "numeric",
    month: "long",
    day: "numeric",
  });

  const lastUpdatedTime = dataUpdatedAt ? formatTime(new Date(dataUpdatedAt)) : null;

  return (
    <div className="w-full space-y-4 pb-4 border-b border-outline-variant">
      {/* Top Bar: Scope Metadata & Refresh Button */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs text-muted-foreground">
        <div className="flex items-center gap-3 flex-wrap">
          <div className="inline-flex items-center gap-1.5 px-2 py-1 text-muted-foreground">
            <Calendar className="w-3.5 h-3.5" />
            <span>{formattedDate}</span>
          </div>

          {lastUpdatedTime && (
            <span className="text-[11px] text-muted-foreground font-mono">
              {t("common.lastUpdated")}: {lastUpdatedTime}
            </span>
          )}
        </div>

        {onRefresh && (
          <button
            onClick={onRefresh}
            disabled={isRefetching}
            aria-label={t("common.refresh")}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-border bg-card hover:bg-secondary text-foreground font-medium transition-colors cursor-pointer disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 text-brand-green-600 ${isRefetching ? "animate-spin" : ""}`} />
            <span>{isRefetching ? t("common.processing") : t("common.refresh")}</span>
          </button>
        )}
      </div>

      {/* Page Title & Operational Subtitle */}
      <div>
        <h1 className="type-headline-small font-semibold text-foreground">
          {t("dashboard.title")}
        </h1>
        <p className="type-body-medium text-muted-foreground mt-1">
          {t("dashboard.subtitle")}
        </p>
      </div>
    </div>
  );
}
