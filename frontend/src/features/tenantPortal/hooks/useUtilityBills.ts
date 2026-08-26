import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ApiError } from "@/shared/lib/http";
import { tenantUtilityBillsApi } from "../api/utilityBills.api";
import type { TenantLinkUtilityAccountRequest, TenantReplaceUtilityAccountRequest, TenantUtilityAccountDto, TenantUtilityTypeName } from "../types/utilityBills.types";
import {
  ensureTransientUtilityVerifications,
  evaluateUtilitySummaryVerificationWindows,
  evaluateUtilityVerificationWindows,
  startUtilityVerification,
  UTILITY_VERIFICATION_INTERVAL_MS,
} from "../utils/utilityVerification";

export const tenantUtilityBillKeys = {
  all: (userId?: string) => ["tenant-utility-bills", userId] as const,
  accounts: (userId?: string) => [...tenantUtilityBillKeys.all(userId), "accounts"] as const,
  bills: (userId: string | undefined, type?: TenantUtilityTypeName) =>
    [...tenantUtilityBillKeys.all(userId), "bills", type ?? "all"] as const,
  summary: (userId?: string) => [...tenantUtilityBillKeys.all(userId), "summary"] as const,
};

function useTenantQueryState() {
  const { user, isAuthenticated } = useAuth();
  return {
    userId: user?.id,
    enabled: isAuthenticated && user?.roleCode === "TENANT" && Boolean(user?.id),
  };
}

function retryUtilityQuery(failureCount: number, error: unknown) {
  if (failureCount >= 1) return false;
  return !(error instanceof ApiError) || error.status >= 500;
}

export function useMyUtilityAccounts() {
  const state = useTenantQueryState();
  const queryClient = useQueryClient();
  return useQuery({
    queryKey: tenantUtilityBillKeys.accounts(state.userId),
    queryFn: tenantUtilityBillsApi.getAccounts,
    enabled: state.enabled,
    staleTime: 300_000,
    retry: retryUtilityQuery,
    refetchInterval: (query) => {
      const accounts = query.state.data ?? [];
      ensureTransientUtilityVerifications(state.userId, accounts);
      const result = evaluateUtilityVerificationWindows(state.userId, accounts);
      if (result.completedSuccessfully) {
        void queryClient.invalidateQueries({
          queryKey: [...tenantUtilityBillKeys.all(state.userId), "bills"],
        });
        void queryClient.invalidateQueries({
          queryKey: tenantUtilityBillKeys.summary(state.userId),
        });
      }
      return result.shouldPoll ? UTILITY_VERIFICATION_INTERVAL_MS : false;
    },
  });
}

export function useMyUtilityBills(type?: TenantUtilityTypeName, enabled = true) {
  const state = useTenantQueryState();
  return useInfiniteQuery({
    queryKey: tenantUtilityBillKeys.bills(state.userId, type),
    queryFn: ({ pageParam }) => tenantUtilityBillsApi.getBills(type, 50, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => page.hasMore ? page.nextCursor ?? undefined : undefined,
    enabled: state.enabled && enabled,
    staleTime: 300_000,
    retry: retryUtilityQuery,
  });
}

export function useUtilityDashboardSummary() {
  const state = useTenantQueryState();
  return useQuery({
    queryKey: tenantUtilityBillKeys.summary(state.userId),
    queryFn: tenantUtilityBillsApi.getDashboardSummary,
    enabled: state.enabled,
    staleTime: 300_000,
    retry: retryUtilityQuery,
    refetchInterval: (query) => evaluateUtilitySummaryVerificationWindows(
      state.userId,
      query.state.data,
    ) ? UTILITY_VERIFICATION_INTERVAL_MS : false,
  });
}

export function useLinkMyUtilityAccount() {
  const state = useTenantQueryState();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: TenantLinkUtilityAccountRequest) => tenantUtilityBillsApi.linkAccount(request),
    onSuccess: (account) => {
      startUtilityVerification(state.userId, account.id, account);
      return queryClient.invalidateQueries({ queryKey: tenantUtilityBillKeys.all(state.userId) });
    },
  });
}

export function useReplaceMyUtilityAccount() {
  const state = useTenantQueryState();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: TenantReplaceUtilityAccountRequest }) =>
      tenantUtilityBillsApi.replaceAccount(id, request),
    onSuccess: (account) => {
      startUtilityVerification(state.userId, account.id, account);
      return queryClient.invalidateQueries({ queryKey: tenantUtilityBillKeys.all(state.userId) });
    },
  });
}

export function useUnlinkMyUtilityAccount() {
  const state = useTenantQueryState();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: tenantUtilityBillsApi.unlinkAccount,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantUtilityBillKeys.all(state.userId) }),
  });
}

export function useRequestMyUtilitySync() {
  const state = useTenantQueryState();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const accounts = queryClient.getQueryData<TenantUtilityAccountDto[]>(
        tenantUtilityBillKeys.accounts(state.userId),
      );
      const baseline = accounts?.find((account) => account.id === id);
      await tenantUtilityBillsApi.requestSync(id);
      return { id, baseline };
    },
    onSuccess: ({ id, baseline }) => {
      startUtilityVerification(state.userId, id, baseline);
      return queryClient.invalidateQueries({ queryKey: tenantUtilityBillKeys.all(state.userId) });
    },
  });
}
