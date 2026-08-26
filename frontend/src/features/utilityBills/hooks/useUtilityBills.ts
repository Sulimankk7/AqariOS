import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { utilityBillsApi } from "../api/utilityBills.api";
import type { LinkUtilityAccountRequest, ReplaceUtilityAccountRequest, UtilityAccountFilters, UtilityBillPaymentStatus } from "../types/utilityBills.types";

export const utilityBillKeys = {
  all: ["utility-bills"] as const,
  accounts: (filters: UtilityAccountFilters) => [...utilityBillKeys.all, "accounts", filters] as const,
  account: (id: string) => [...utilityBillKeys.all, "account", id] as const,
  bills: (id: string, status?: UtilityBillPaymentStatus) => [...utilityBillKeys.all, "bills", id, status ?? "all"] as const,
};

export function useUtilityAccounts(filters: UtilityAccountFilters) {
  return useInfiniteQuery({
    queryKey: utilityBillKeys.accounts(filters),
    queryFn: ({ pageParam }) => utilityBillsApi.getAccounts(filters, pageParam, 25),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => page.hasMore ? page.nextCursor ?? undefined : undefined,
  });
}
export function useUtilityAccount(id?: string) {
  return useQuery({ queryKey: utilityBillKeys.account(id ?? ""), queryFn: () => utilityBillsApi.getAccount(id!), enabled: !!id });
}
export function useUtilityAccountBills(id?: string, status?: UtilityBillPaymentStatus) {
  return useInfiniteQuery({
    queryKey: utilityBillKeys.bills(id ?? "", status),
    queryFn: ({ pageParam }) => utilityBillsApi.getBills(id!, status, pageParam, 25),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => page.hasMore ? page.nextCursor ?? undefined : undefined,
    enabled: !!id,
  });
}
export function useLinkUtilityAccount() {
  const client = useQueryClient();
  return useMutation({ mutationFn: (request: LinkUtilityAccountRequest) => utilityBillsApi.linkAccount(request), onSuccess: () => client.invalidateQueries({ queryKey: utilityBillKeys.all }) });
}
export function useUnlinkUtilityAccount() {
  const client = useQueryClient();
  return useMutation({ mutationFn: (id: string) => utilityBillsApi.unlinkAccount(id), onSuccess: () => client.invalidateQueries({ queryKey: utilityBillKeys.all }) });
}
export function useReplaceUtilityAccount() {
  const client = useQueryClient();
  return useMutation({ mutationFn: ({ id, request }: { id: string; request: ReplaceUtilityAccountRequest }) => utilityBillsApi.replaceAccount(id, request), onSuccess: () => client.invalidateQueries({ queryKey: utilityBillKeys.all }) });
}
export function useRequestUtilitySync() {
  const client = useQueryClient();
  return useMutation({ mutationFn: (id: string) => utilityBillsApi.requestSync(id), onSuccess: (_, id) => { client.invalidateQueries({ queryKey: utilityBillKeys.account(id) }); client.invalidateQueries({ queryKey: utilityBillKeys.all }); } });
}
