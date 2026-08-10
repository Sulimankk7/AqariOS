import { http } from '@/shared/lib/http';
import { KeysetPage, PaymentVerificationQueueItem, RentPaymentDetail } from '../types/payments.types';

export const paymentsApi = {
  getPendingVerifications: async (cursor?: string | null, pageSize: number = 50): Promise<KeysetPage<PaymentVerificationQueueItem>> => {
    const params = new URLSearchParams();
    if (cursor) {
      params.append('cursor', cursor);
    }
    params.append('pageSize', pageSize.toString());

    return await http.get(`/api/v1/owner/payments/pending-verifications?${params.toString()}`);
  },

  getPaymentDetails: async (paymentId: string): Promise<RentPaymentDetail> => {
    return await http.get(`/api/v1/rent-payments/${paymentId}`);
  },

  approveSubmission: async (paymentId: string, submissionId: string): Promise<void> => {
    await http.post(`/api/v1/owner/payments/${paymentId}/submissions/${submissionId}/approve`);
  },

  rejectSubmission: async (paymentId: string, submissionId: string, reason: string): Promise<void> => {
    await http.post(`/api/v1/owner/payments/${paymentId}/submissions/${submissionId}/reject`, { reason });
  },
};
