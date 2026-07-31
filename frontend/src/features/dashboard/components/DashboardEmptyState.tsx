/**
 * DashboardEmptyState Component — Empty state view when portfolio has no registered buildings.
 * Uses namespaced translations from useTranslation().
 */

import React from "react";
import { useNavigate } from "react-router";
import { Building2, Plus } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export function DashboardEmptyState() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="w-full flex flex-col items-center justify-center p-12 text-center rounded-xl border border-dashed border-border bg-card space-y-4">
      <div className="p-4 rounded-full bg-secondary text-brand-green-600">
        <Building2 className="w-8 h-8" />
      </div>
      <div className="max-w-md space-y-1">
        <h3 className="text-lg font-bold text-foreground">
          {t("dashboard.emptyTitle")}
        </h3>
        <p className="text-xs text-muted-foreground">
          {t("dashboard.emptyDescription")}
        </p>
      </div>
      <button
        onClick={() => navigate("/buildings")}
        className="flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-xs font-semibold hover:opacity-90 transition-opacity cursor-pointer shadow-xs"
      >
        <Plus className="w-4 h-4" />
        <span>{t("dashboard.addFirstBuilding")}</span>
      </button>
    </div>
  );
}
