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

export function DashboardKpiSection() {
  const { data, isLoading, isError, error } = useDashboardKPIs({
    staleTime: 300000, // 5 minutes
    maxRetries: 2,
  });

  const formatCurrency = (amount: number, currency = "JOD") => {
    try {
      return `${currency} ${amount.toLocaleString("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      })}`;
    } catch {
      return `${currency} ${amount.toFixed(2)}`;
    }
  };

  return (
    <section className="w-full space-y-4">
      {/* Section Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            Portfolio Performance
          </h2>
          <p className="text-xs text-muted-foreground">
            Real-time real estate analytics & financial metrics from backend
          </p>
        </div>
      </div>

      {/* Responsive KPI Grid: Mobile 1 | Tablet 3 | Desktop 6 */}
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">
        {/* 1. Buildings */}
        <KpiCard
          title="Buildings"
          value={data ? data.property.totalBuildings : undefined}
          subtitle="Registered properties"
          iconType="building"
          isLoading={isLoading}
          isError={isError}
          errorMessage={error || undefined}
        />

        {/* 2. Apartments */}
        <KpiCard
          title="Apartments"
          value={data ? data.property.totalApartments : undefined}
          subtitle={
            data
              ? `${data.property.occupiedApartments} Occupied (${data.property.occupancyRate}%)`
              : "Total residential units"
          }
          iconType="apartments"
          isLoading={isLoading}
          isError={isError}
          errorMessage={error || undefined}
        />

        {/* 3. Lease Contracts */}
        <KpiCard
          title="Lease Contracts"
          value={data ? data.leasing.activeLeases : undefined}
          subtitle={
            data && data.leasing.expiringIn30Days > 0
              ? `${data.leasing.expiringIn30Days} expiring soon`
              : "Active agreements"
          }
          iconType="leases"
          isLoading={isLoading}
          isError={isError}
          errorMessage={error || undefined}
        />

        {/* 4. Tenants */}
        <KpiCard
          title="Tenants"
          value={data ? data.leasing.activeLeases : undefined}
          subtitle="Occupying occupants"
          iconType="tenants"
          isLoading={isLoading}
          isError={isError}
          errorMessage={error || undefined}
        />

        {/* 5. Rent Collection */}
        <KpiCard
          title="Rent Collection"
          value={data ? formatCurrency(data.payments.collectedThisMonth) : undefined}
          subtitle={
            data && data.payments.overduePayments > 0
              ? `${data.payments.overduePayments} overdue payments`
              : "Collected this month"
          }
          iconType="rent"
          isLoading={isLoading}
          isError={isError}
          errorMessage={error || undefined}
        />

        {/* 6. Expenses */}
        <KpiCard
          title="Expenses"
          value={data ? formatCurrency(data.financials.expensesThisMonth) : undefined}
          subtitle="Expenses this month"
          iconType="expenses"
          isLoading={isLoading}
          isError={isError}
          errorMessage={error || undefined}
        />
      </div>
    </section>
  );
}
