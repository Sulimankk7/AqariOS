/**
 * DashboardSkeleton Component — Loading state skeleton using pure semantic classes.
 */

import React from "react";
import { useTranslation } from "@/shared/i18n";

export function DashboardSkeleton() {
  const { t } = useTranslation();
  return (
    <div
      aria-label={t("common.loadingDashboardMetrics")}
      className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 animate-pulse"
    >
      {Array.from({ length: 12 }).map((_, index) => (
        <div
          key={index}
          className="p-5 rounded-lg border border-border bg-card space-y-4"
        >
          <div className="flex items-center justify-between">
            <div className="h-4 w-28 bg-muted rounded-md" />
            <div className="h-8 w-8 bg-muted rounded-md" />
          </div>
          <div className="space-y-2">
            <div className="h-7 w-24 bg-muted rounded-md" />
            <div className="h-3 w-32 bg-muted/60 rounded-md" />
          </div>
        </div>
      ))}
    </div>
  );
}
