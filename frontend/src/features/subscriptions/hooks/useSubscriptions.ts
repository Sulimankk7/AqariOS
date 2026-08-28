import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { subscriptionsApi } from "../api/subscriptions.api";
import type { ApprovePlanChangeRequestRequest, CreateCompanySubscriptionRequest, CreatePlanChangeRequest, CreatePlanRequest, RejectPlanChangeRequestRequest } from "../types/subscriptions.types";

export const subscriptionKeys = {
  all: ["subscriptions"] as const,
  company: () => [...subscriptionKeys.all, "company"] as const,
  availablePlans: (page: number, pageSize: number) => [...subscriptionKeys.company(), "plans", page, pageSize] as const,
  mine: () => [...subscriptionKeys.company(), "current"] as const,
  myRequests: (page: number, pageSize: number) => [...subscriptionKeys.company(), "requests", page, pageSize] as const,
  platform: () => [...subscriptionKeys.all, "platform"] as const,
  platformPlans: (filters: object) => [...subscriptionKeys.platform(), "plans", filters] as const,
  platformPlan: (id: string) => [...subscriptionKeys.platform(), "plan", id] as const,
  platformSubscriptions: (filters: object) => [...subscriptionKeys.platform(), "subscriptions", filters] as const,
  platformCompanies: () => [...subscriptionKeys.platform(), "companies"] as const,
  platformSubscription: (id: string) => [...subscriptionKeys.platform(), "subscription", id] as const,
  platformRequests: (filters: object) => [...subscriptionKeys.platform(), "requests", filters] as const,
  platformRequest: (id: string) => [...subscriptionKeys.platform(), "request", id] as const,
};

export const useAvailablePlans = (page: number, pageSize: number) => useQuery({ queryKey: subscriptionKeys.availablePlans(page, pageSize), queryFn: () => subscriptionsApi.getAvailablePlans(page, pageSize) });
export const useMySubscription = () => useQuery({ queryKey: subscriptionKeys.mine(), queryFn: subscriptionsApi.getMySubscription, retry: (count, error: any) => error?.status === 404 ? false : count < 2 });
export const useMyPlanChangeRequests = (page: number, pageSize: number) => useQuery({ queryKey: subscriptionKeys.myRequests(page, pageSize), queryFn: () => subscriptionsApi.getMyRequests(page, pageSize) });

export function useCreateMyPlanChangeRequest() { const client = useQueryClient(); return useMutation({ mutationFn: (request: CreatePlanChangeRequest) => subscriptionsApi.createMyRequest(request), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.company() }) }); }
export function useCancelMyPlanChangeRequest() { const client = useQueryClient(); return useMutation({ mutationFn: (id: string) => subscriptionsApi.cancelMyRequest(id), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.company() }) }); }

export const usePlatformPlans = (filters: { page: number; pageSize: number; isActive?: boolean; search?: string }) => useQuery({ queryKey: subscriptionKeys.platformPlans(filters), queryFn: () => subscriptionsApi.getPlatformPlans(filters) });
export const usePlatformPlan = (id: string | null) => useQuery({ queryKey: subscriptionKeys.platformPlan(id ?? ""), queryFn: () => subscriptionsApi.getPlatformPlan(id!), enabled: Boolean(id), retry: (count, error: any) => error?.status === 404 ? false : count < 2 });
export function useCreatePlatformPlan() { const client = useQueryClient(); return useMutation({ mutationFn: (request: CreatePlanRequest) => subscriptionsApi.createPlatformPlan(request), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.platform() }) }); }
export function useSetPlatformPlanActive() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, active }: { id: string; active: boolean }) => subscriptionsApi.setPlatformPlanActive(id, active), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.platform() }) }); }

export const usePlatformSubscriptions = (filters: Record<string, string | number | boolean | undefined | null>) => useQuery({ queryKey: subscriptionKeys.platformSubscriptions(filters), queryFn: () => subscriptionsApi.getPlatformSubscriptions(filters) });
export const usePlatformCompanies = () => useQuery({ queryKey: subscriptionKeys.platformCompanies(), queryFn: subscriptionsApi.getPlatformCompanies });
export const usePlatformSubscription = (id: string | null) => useQuery({ queryKey: subscriptionKeys.platformSubscription(id ?? ""), queryFn: () => subscriptionsApi.getPlatformSubscription(id!), enabled: Boolean(id), retry: (count, error: any) => error?.status === 404 ? false : count < 2 });
export function useCreatePlatformSubscription() { const client = useQueryClient(); return useMutation({ mutationFn: (request: CreateCompanySubscriptionRequest) => subscriptionsApi.createPlatformSubscription(request), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.platform() }) }); }

export const usePlatformPlanChangeRequests = (filters: Record<string, string | number | boolean | undefined | null>) => useQuery({ queryKey: subscriptionKeys.platformRequests(filters), queryFn: () => subscriptionsApi.getPlatformRequests(filters) });
export const usePlatformPlanChangeRequest = (id: string | null) => useQuery({ queryKey: subscriptionKeys.platformRequest(id ?? ""), queryFn: () => subscriptionsApi.getPlatformRequest(id!), enabled: Boolean(id), retry: (count, error: any) => error?.status === 404 ? false : count < 2 });
export function useApprovePlatformPlanChangeRequest() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, request }: { id: string; request: ApprovePlanChangeRequestRequest }) => subscriptionsApi.approvePlatformRequest(id, request), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.platform() }) }); }
export function useRejectPlatformPlanChangeRequest() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, request }: { id: string; request: RejectPlanChangeRequestRequest }) => subscriptionsApi.rejectPlatformRequest(id, request), onSuccess: () => client.invalidateQueries({ queryKey: subscriptionKeys.platform() }) }); }
