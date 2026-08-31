/**
 * DashboardPage Component — Production Dashboard.
 *
 * Backed exclusively by GET /api/v1/dashboard/summary.
 * Never invents backend data or fake charts.
 */

import React from "react";
import { useDashboardSummary } from "../hooks/useDashboardSummary";
import { DashboardHeader } from "../components/DashboardHeader";
import { KpiGrid } from "../components/KpiGrid";
import { QuickActions } from "../components/QuickActions";
import { DashboardEmptyState } from "../components/DashboardEmptyState";
import { DashboardSkeleton } from "../components/DashboardSkeleton";
import { DashboardError } from "../components/DashboardError";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import { PaygUsageSection } from "../components/PaygUsageSection";

export function DashboardPage() {
  const {
    data,
    isLoading,
    isError,
    error,
    refetch,
    isRefetching,
    dataUpdatedAt,
  } = useDashboardSummary();

  const isEmptyPortfolio = data && data.property.totalBuildings === 0;

  return (
    <PageContainer>
      {/* Production Dashboard Header with Live Refresh */}
      <DashboardHeader
        onRefresh={() => refetch()}
        isRefetching={isRefetching}
        dataUpdatedAt={dataUpdatedAt}
      />

      {/* Quick Operations Bar */}
      <QuickActions />

      <PaygUsageSection />

      {/* Loading Skeleton View */}
      {isLoading && <DashboardSkeleton />}

      {/* Error State View */}
      {isError && (
        <DashboardError
          message={extractUserFriendlyError(error)}
          onRetry={() => refetch()}
        />
      )}

      {/* Empty Portfolio State */}
      {!isLoading && !isError && isEmptyPortfolio && (
        <DashboardEmptyState />
      )}

      {/* Successful KPI Grid Render */}
      {!isLoading && !isError && data && !isEmptyPortfolio && (
        <KpiGrid data={data} />
      )}
    </PageContainer>
  );
}

export default DashboardPage;
