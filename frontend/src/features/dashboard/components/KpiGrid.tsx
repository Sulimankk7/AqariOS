/**
 * KpiGrid Component — 4-column responsive grid layout.
 *
 * Routes: all card paths use centralized ROUTES constants.
 * Property KPIs → /buildings
 * Leasing KPIs  → /leases
 * Payment KPIs  → /payments
 * Financial KPI → /financial-operations
 */

import React from "react";
import {
  Building2,
  Home,
  Users,
  FileText,
  Clock,
  FilePlus,
  Wallet,
  CircleDollarSign,
  AlertTriangle,
  Receipt,
  Building,
} from "lucide-react";
import type { DashboardSummaryDto } from "../types/dashboard.types";
import { KpiCard } from "./KpiCard";
import { useTranslation } from "@/shared/i18n";
import { ROUTES } from "@/config/routes";

interface KpiGridProps {
  data: DashboardSummaryDto;
}

export function KpiGrid({ data }: KpiGridProps) {
  const { t, formatCurrency } = useTranslation();
  const { property, leasing, payments, financials } = data;

  return (
    <div className="w-full space-y-8">

      {/* ── Section 1: Property Portfolio ── */}
      <section aria-label={t("dashboard.propertyPortfolio")}>
        <h2 className="type-label-medium text-on-surface-variant uppercase mb-3">
          {t("dashboard.propertyPortfolio")}
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            title={t("properties.totalBuildings")}
            value={property.totalBuildings}
            description={t("properties.building", { count: property.totalBuildings })}
            icon={Building2}
            variant="default"
            path={ROUTES.buildings.root}
          />
          <KpiCard
            title={t("properties.totalApartments")}
            value={property.totalApartments}
            description={t("properties.apartment", { count: property.totalApartments })}
            icon={Home}
            variant="default"
            path={ROUTES.apartments.root}
          />
          <KpiCard
            title={t("properties.occupancyRate")}
            value={`${property.occupancyRate}%`}
            description={t("properties.occupiedApartments")}
            icon={Users}
            variant={property.occupancyRate >= 80 ? "success" : "warning"}
            path={ROUTES.buildings.root}
          />
          <KpiCard
            title={t("properties.occupiedApartments")}
            value={property.occupiedApartments}
            description={t("properties.apartment", { count: property.occupiedApartments })}
            icon={Home}
            variant="info"
            path={ROUTES.apartments.root}
          />
          <KpiCard
            title={t("properties.vacantApartments")}
            value={property.vacantApartments}
            description={t("properties.apartment", { count: property.vacantApartments })}
            icon={Building}
            variant="default"
            path={ROUTES.apartments.root}
          />
        </div>
      </section>

      {/* ── Section 2: Leasing ── */}
      <section aria-label={t("dashboard.leasingAgreements")}>
        <h2 className="type-label-medium text-on-surface-variant uppercase mb-3">
          {t("dashboard.leasingAgreements")}
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            title={t("leasing.activeLeases")}
            value={leasing.activeLeases}
            description={t("leasing.lease", { count: leasing.activeLeases })}
            icon={FileText}
            variant="info"
            path={ROUTES.leases.root}
          />
          <KpiCard
            title={t("leasing.expiringIn30Days")}
            value={leasing.expiringIn30Days}
            description={t("leasing.lease", { count: leasing.expiringIn30Days })}
            icon={Clock}
            variant={leasing.expiringIn30Days > 0 ? "warning" : "default"}
            path={ROUTES.leases.root}
          />
          <KpiCard
            title={t("leasing.newLeasesThisMonth")}
            value={leasing.newLeasesThisMonth}
            description={t("leasing.lease", { count: leasing.newLeasesThisMonth })}
            icon={FilePlus}
            variant="success"
            path={ROUTES.leases.root}
          />
        </div>
      </section>

      {/* ── Section 3: Financial Collections ── */}
      <section aria-label={t("dashboard.paymentsFinancials")}>
        <h2 className="type-label-medium text-on-surface-variant uppercase mb-3">
          {t("dashboard.paymentsFinancials")}
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiCard
            title={t("financials.collectedThisMonth")}
            value={formatCurrency(payments.collectedThisMonth)}
            description={t("financials.collectedThisMonth")}
            icon={Wallet}
            variant="success"
            path={ROUTES.payments.root}
          />
          <KpiCard
            title={t("financials.outstandingAmount")}
            value={formatCurrency(payments.outstandingAmount)}
            description={t("financials.outstandingAmount")}
            icon={CircleDollarSign}
            variant={payments.outstandingAmount > 0 ? "warning" : "default"}
            path={ROUTES.payments.root}
          />
          <KpiCard
            title={t("financials.overduePayments")}
            value={payments.overduePayments}
            description={t("financials.payment", { count: payments.overduePayments })}
            icon={AlertTriangle}
            variant={payments.overduePayments > 0 ? "danger" : "default"}
            path={ROUTES.payments.root}
          />
          <KpiCard
            title={t("financials.expensesThisMonth")}
            value={formatCurrency(financials.expensesThisMonth)}
            description={t("financials.expensesThisMonth")}
            icon={Receipt}
            variant="default"
            path={ROUTES.financialOperations.root}
          />
        </div>
      </section>

    </div>
  );
}
