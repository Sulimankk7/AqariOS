/**
 * useDashboardKPIs — Data hook for Dashboard KPIs.
 *
 * Consumes GET /api/v1/dashboard/summary.
 * Implements:
 * - In-memory caching
 * - Automatic retry (up to 2 retries)
 * - Stale time management (5 minutes)
 * - refetchOnWindowFocus = false
 */

import { useState, useEffect, useCallback, useRef } from "react";
import { dashboardApi } from "../api/dashboard.api";
import type { DashboardSummaryDto } from "../types/dashboard.types";
import { extractUserFriendlyError } from "@/shared/utils";
import { translateCurrent } from "@/shared/i18n/runtime";

interface UseDashboardKPIsOptions {
  staleTime?: number; // Cache validity duration in ms (default 5 min)
  maxRetries?: number; // Retry attempts on failure (default 2)
}

interface UseDashboardKPIsState {
  data: DashboardSummaryDto | null;
  isLoading: boolean;
  isError: boolean;
  error: string | null;
}

// In-memory global cache store
let cachedKpiData: { data: DashboardSummaryDto; timestamp: number } | null = null;

export function useDashboardKPIs(options: UseDashboardKPIsOptions = {}) {
  const { staleTime = 300000, maxRetries = 2 } = options;

  const [state, setState] = useState<UseDashboardKPIsState>(() => {
    if (cachedKpiData && Date.now() - cachedKpiData.timestamp < staleTime) {
      return {
        data: cachedKpiData.data,
        isLoading: false,
        isError: false,
        error: null,
      };
    }
    return {
      data: null,
      isLoading: true,
      isError: false,
      error: null,
    };
  });

  const isMountedRef = useRef(true);

  const fetchKpis = useCallback(async (retryCount = 0) => {
    if (!isMountedRef.current) return;

    if (cachedKpiData && Date.now() - cachedKpiData.timestamp < staleTime) {
      setState({
        data: cachedKpiData.data,
        isLoading: false,
        isError: false,
        error: null,
      });
      return;
    }

    setState((prev) => ({ ...prev, isLoading: true, isError: false, error: null }));

    try {
      const summary = await dashboardApi.getSummary();
      cachedKpiData = { data: summary, timestamp: Date.now() };

      if (isMountedRef.current) {
        setState({
          data: summary,
          isLoading: false,
          isError: false,
          error: null,
        });
      }
    } catch (err) {
      if (retryCount < maxRetries) {
        setTimeout(() => {
          fetchKpis(retryCount + 1);
        }, 1000 * Math.pow(2, retryCount));
        return;
      }

      if (isMountedRef.current) {
        const errorMsg = extractUserFriendlyError(err, translateCurrent("dashboard.loadError"));

        setState({
          data: null,
          isLoading: false,
          isError: true,
          error: errorMsg,
        });
      }
    }
  }, [staleTime, maxRetries]);

  useEffect(() => {
    isMountedRef.current = true;
    fetchKpis();

    return () => {
      isMountedRef.current = false;
    };
  }, [fetchKpis]);

  const refetch = useCallback(() => {
    cachedKpiData = null;
    return fetchKpis(0);
  }, [fetchKpis]);

  return {
    data: state.data,
    isLoading: state.isLoading,
    isError: state.isError,
    error: state.error,
    refetch,
  };
}
