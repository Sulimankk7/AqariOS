/**
 * Dashboard API Client — Consumes GET /api/v1/dashboard/summary
 */

import { http } from "@/shared/lib/http";
import type { DashboardSummaryDto } from "../types/dashboard.types";

export const dashboardApi = {
  async getSummary(): Promise<DashboardSummaryDto> {
    return await http.get<DashboardSummaryDto>("/api/v1/dashboard/summary");
  },
};
