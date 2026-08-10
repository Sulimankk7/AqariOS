import { useInfiniteQuery, useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { paymentsApi } from '../api/payments.api';

export const paymentsKeys = {
  all: ['payments'] as const,
  verifications: () => [...paymentsKeys.all, 'verifications'] as const,
  details: (id: string) => [...paymentsKeys.all, 'details', id] as const,
};

export function useVerificationQueue() {
  return useInfiniteQuery({
    queryKey: paymentsKeys.verifications(),
    queryFn: ({ pageParam }) => paymentsApi.getPendingVerifications(pageParam as string | undefined, 50),
    getNextPageParam: (lastPage) => lastPage.hasMore ? lastPage.nextCursor : undefined,
    initialPageParam: undefined as string | undefined,
  });
}

export function usePaymentDetails(paymentId: string | null) {
  return useQuery({
    queryKey: paymentsKeys.details(paymentId!),
    queryFn: () => paymentsApi.getPaymentDetails(paymentId!),
    enabled: !!paymentId,
  });
}

export function useApproveSubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ paymentId, submissionId }: { paymentId: string; submissionId: string }) => 
      paymentsApi.approveSubmission(paymentId, submissionId),
    onSuccess: (_, { paymentId }) => {
      queryClient.invalidateQueries({ queryKey: paymentsKeys.verifications() });
      queryClient.invalidateQueries({ queryKey: paymentsKeys.details(paymentId) });
    },
  });
}

export function useRejectSubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ paymentId, submissionId, reason }: { paymentId: string; submissionId: string; reason: string }) => 
      paymentsApi.rejectSubmission(paymentId, submissionId, reason),
    onSuccess: (_, { paymentId }) => {
      queryClient.invalidateQueries({ queryKey: paymentsKeys.verifications() });
      queryClient.invalidateQueries({ queryKey: paymentsKeys.details(paymentId) });
    },
  });
}
