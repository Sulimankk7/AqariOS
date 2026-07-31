/**
 * QuickActions Component — Enterprise SaaS quick operations bar.
 *
 * All navigation paths use centralized ROUTES constants.
 * Primary action:   Add Building → /buildings
 * Secondary:        Create Lease → /leases
 *                   Record Payment → /payments
 *                   Report Maintenance → /maintenance
 *                   Export Summary → /financial-operations
 */

import React from "react";
import { useNavigate } from "react-router";
import { Plus, FilePlus, DollarSign, Wrench, Download } from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { ROUTES } from "@/config/routes";

export function QuickActions() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const actions = [
    {
      id: "add-building",
      label: t("dashboard.addBuilding"),
      icon: Plus,
      path: ROUTES.buildings.root,
      isPrimary: true,
    },
    {
      id: "create-lease",
      label: t("dashboard.createLease"),
      icon: FilePlus,
      path: ROUTES.leases.root,
      isPrimary: false,
    },
    {
      id: "record-payment",
      label: t("dashboard.recordPayment"),
      icon: DollarSign,
      path: ROUTES.payments.root,
      isPrimary: false,
    },
    {
      id: "report-maintenance",
      label: t("dashboard.reportMaintenance"),
      icon: Wrench,
      path: ROUTES.maintenance.root,
      isPrimary: false,
    },
    {
      id: "export-summary",
      label: t("dashboard.exportSummary"),
      icon: Download,
      path: ROUTES.financialOperations.root,
      isPrimary: false,
    },
  ];

  return (
    <section aria-label="Quick Actions" className="w-full space-y-3 pt-2">
      <h2 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
        {t("dashboard.quickOperations")}
      </h2>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3">
        {actions.map((act) => {
          const Icon = act.icon;
          return (
            <button
              key={act.id}
              id={`quick-action-${act.id}`}
              onClick={() => navigate(act.path)}
              aria-label={act.label}
              className={`flex flex-col items-center justify-center p-3 rounded-lg border text-foreground text-xs font-medium transition-all duration-150 cursor-pointer space-y-2 group focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
                act.isPrimary
                  ? "border-primary/40 bg-primary/5 hover:bg-primary hover:text-primary-foreground hover:border-primary"
                  : "border-border bg-card hover:bg-secondary hover:border-border-strong"
              }`}
            >
              <div
                className={`p-2 rounded-md transition-colors ${
                  act.isPrimary
                    ? "bg-primary/10 group-hover:bg-white/20"
                    : "bg-secondary group-hover:bg-primary group-hover:text-primary-foreground"
                }`}
              >
                <Icon
                  className={`w-4 h-4 ${
                    act.isPrimary
                      ? "text-primary group-hover:text-white"
                      : "text-brand-green-600 group-hover:text-primary-foreground"
                  }`}
                />
              </div>
              <span className="text-center font-medium truncate w-full">{act.label}</span>
            </button>
          );
        })}
      </div>
    </section>
  );
}
