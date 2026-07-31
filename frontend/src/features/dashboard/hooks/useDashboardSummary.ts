/**
 * useDashboardSummary — React Query Hook for Dashboard KPI Overview.
 */

import { useQuery } from "@tanstack/react-query";
import { dashboardApi } from "../api/dashboard.api";
import type { DashboardSummaryDto } from "../types/dashboard.types";

export function useDashboardSummary() {
  return useQuery<DashboardSummaryDto, Error>({
    queryKey: ["dashboard", "summary"],
    queryFn: () => dashboardApi.getSummary(),
    staleTime: 300000, // 5 minutes cache
    refetchOnWindowFocus: false,
    retry: 2,
  });
}
