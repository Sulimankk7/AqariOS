import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { tenantPortalApi } from "../api/tenantPortal.api";
import { useAuth } from "@/features/auth/hooks/useAuth";
import type { SubmitPaymentVerificationRequestPayload } from "../types/tenantPortal.types";

/**
 * Query hook to fetch rent payments for the currently authenticated tenant.
 * Calls GET /api/v1/tenant-portal/payments — identity resolved server-side.
 */
export function useMyPayments() {
  const { user } = useAuth();
  const userId = user?.id;

  return useQuery({
    queryKey: ["tenant", userId, "payments"],
    queryFn: () => tenantPortalApi.getMyPayments(),
    enabled: !!userId,
    staleTime: 3 * 60 * 1000, // 3 minutes
  });
}

/**
 * Mutation hook for tenant payment proof verification submission.
 * Invalidates user/tenant notifications and portal cache upon success.
 */
export function useSubmitPaymentVerification() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const userId = user?.id;

  return useMutation({
    mutationFn: ({
      rentPaymentId,
      payload,
    }: {
      rentPaymentId: string;
      payload: SubmitPaymentVerificationRequestPayload;
    }) => tenantPortalApi.submitPaymentVerification(rentPaymentId, payload),
    onSuccess: () => {
      // Invalidate tenant notifications and payment list cache
      if (userId) {
        queryClient.invalidateQueries({
          queryKey: ["tenant", userId],
        });
      }
    },
  });
}

