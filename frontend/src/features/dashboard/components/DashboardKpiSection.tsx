/**
 * DashboardKpiSection — Main responsive KPI section for AqariOS Dashboard.
 *
 * Responsive Layout:
 * - Desktop: 6 cards (xl:grid-cols-6 / lg:grid-cols-3)
 * - Tablet: 3 cards (md:grid-cols-3)
 * - Mobile: 1 card (grid-cols-1)
 *
 * Consumes single backend endpoint GET /api/v1/dashboard/summary via useDashboardKPIs hook.
 */

import React from "react";
import { useDashboardKPIs } from "../hooks/useDashboardKPIs";
import { KpiCard } from "./KpiCard";
import { useTranslation } from "@/shared/i18n";
import { Building2, Home, FileText, Users, Wallet, Receipt } from "lucide-react";

export function DashboardKpiSection() {
  const { t, formatCurrency } = useTranslation();
  const { data, isLoading, isError, error } = useDashboardKPIs({
    staleTime: 300000, // 5 minutes
    maxRetries: 2,
  });

  return (
    <section className="w-full space-y-4">
      {/* Section Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("dashboard.performanceTitle")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("dashboard.performanceSubtitle")}
          </p>
        </div>
      </div>

      {/* Responsive KPI Grid: Mobile 1 | Tablet 3 | Desktop 6 */}
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">
        {/* 1. Buildings */}
        <KpiCard
          title={t("properties.totalBuildings")}
          value={data?.property.totalBuildings ?? "—"}
          description={t("dashboard.registeredProperties")}
          icon={Building2}
        />

        {/* 2. Apartments */}
        <KpiCard
          title={t("properties.totalApartments")}
          value={data?.property.totalApartments ?? "—"}
          description={
            data
              ? t("dashboard.occupiedSummary", { count: data.property.occupiedApartments, rate: data.property.occupancyRate })
              : t("dashboard.totalResidentialUnits")
          }
          icon={Home}
        />

        {/* 3. Lease Contracts */}
        <KpiCard
          title={t("leasing.activeLeases")}
          value={data?.leasing.activeLeases ?? "—"}
          description={
            data && data.leasing.expiringIn30Days > 0
              ? t("dashboard.expiringSoon", { count: data.leasing.expiringIn30Days })
              : t("dashboard.activeAgreements")
          }
          icon={FileText}
        />

        {/* 4. Tenants */}
        <KpiCard
          title={t("leasing.tenant", { count: 2 })}
          value={data?.leasing.activeLeases ?? "—"}
          description={t("dashboard.occupyingTenants")}
          icon={Users}
        />

        {/* 5. Rent Collection */}
        <KpiCard
          title={t("financials.collectedThisMonth")}
          value={data ? formatCurrency(data.payments.collectedThisMonth) : "—"}
          description={
            data && data.payments.overduePayments > 0
              ? t("dashboard.overduePaymentsCount", { count: data.payments.overduePayments })
              : t("dashboard.collectedThisMonthDescription")
          }
          icon={Wallet}
        />

        {/* 6. Expenses */}
        <KpiCard
          title={t("financials.expensesThisMonth")}
          value={data ? formatCurrency(data.financials.expensesThisMonth) : "—"}
          description={t("dashboard.expensesThisMonthDescription")}
          icon={Receipt}
        />
      </div>
    </section>
  );
}
