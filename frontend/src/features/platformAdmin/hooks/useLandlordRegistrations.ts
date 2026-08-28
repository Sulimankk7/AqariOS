import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { platformAdminApi } from "../api/platformAdmin.api";

export const platformAdminKeys = {
  all: ["platform"] as const,
  registrations: () => [...platformAdminKeys.all, "landlordRegistrations"] as const,
  pending: (page: number, pageSize: number) => [...platformAdminKeys.registrations(), "pending", page, pageSize] as const,
  detail: (id: string) => [...platformAdminKeys.registrations(), "detail", id] as const,
};

export function usePendingLandlordRegistrations(page: number, pageSize: number) {
  return useQuery({
    queryKey: platformAdminKeys.pending(page, pageSize),
    queryFn: () => platformAdminApi.getPending(page, pageSize),
  });
}

export function useLandlordRegistration(id: string | null) {
  return useQuery({
    queryKey: platformAdminKeys.detail(id ?? ""),
    queryFn: () => platformAdminApi.getRegistration(id!),
    enabled: Boolean(id),
    retry: (count, error: any) => error?.status === 404 ? false : count < 2,
  });
}

export function useApproveLandlordRegistration() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => platformAdminApi.approve(id),
    onSuccess: (result) => {
      client.removeQueries({ queryKey: platformAdminKeys.detail(result.registrationId) });
      client.invalidateQueries({ queryKey: platformAdminKeys.registrations() });
    },
  });
}

export function useRejectLandlordRegistration() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => platformAdminApi.reject(id, { reason: reason.trim() }),
    onSuccess: (result) => {
      client.removeQueries({ queryKey: platformAdminKeys.detail(result.registrationId) });
      client.invalidateQueries({ queryKey: platformAdminKeys.registrations() });
    },
  });
}
