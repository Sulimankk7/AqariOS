import { http } from '@/shared/lib/http';
import type { RentPaymentDto, RentPaymentFilterParams, RemindRentPaymentResponseDto } from '../types/financials.types';
import type { RentPaymentDetail, RentPaymentReceiptDto } from '@/features/payments/types/payments.types';

export const financialsApi = {
  getRentPayments: async (params: RentPaymentFilterParams = {}): Promise<RentPaymentDto[]> => {
    const query = new URLSearchParams();
    if (params.buildingId) {
      query.append('buildingId', params.buildingId);
    }
    if (params.status !== undefined && params.status !== null && params.status !== '') {
      query.append('status', String(params.status));
    }
    if (params.dateFrom) {
      query.append('dateFrom', params.dateFrom);
    }
    if (params.dateTo) {
      query.append('dateTo', params.dateTo);
    }
    if (params.searchTerm && params.searchTerm.trim()) {
      query.append('searchTerm', params.searchTerm.trim());
    }
    if (params.lastSeenId) {
      query.append('lastSeenId', params.lastSeenId);
    }
    if (params.lastSeenDueDate) {
      query.append('lastSeenDueDate', params.lastSeenDueDate);
    }
    if (params.pageSize) {
      query.append('pageSize', String(params.pageSize));
    }

    const qs = query.toString();
    return await http.get<RentPaymentDto[]>(`/api/v1/rent-payments${qs ? `?${qs}` : ''}`);
  },

  getPaymentDetails: async (paymentId: string): Promise<RentPaymentDetail> => {
    return await http.get<RentPaymentDetail>(`/api/v1/rent-payments/${paymentId}`);
  },

  getReceiptByRentPaymentId: async (paymentId: string): Promise<RentPaymentReceiptDto | null> => {
    try {
      return await http.get<RentPaymentReceiptDto>(`/api/v1/rent-payments/${paymentId}/receipt`);
    } catch (err: any) {
      if (err?.status === 404 || err?.statusCode === 404) {
        return null;
      }
      throw err;
    }
  },

  remindTenant: async (paymentId: string): Promise<RemindRentPaymentResponseDto> => {
    return await http.post<RemindRentPaymentResponseDto>(`/api/v1/rent-payments/${paymentId}/remind`, {});
  },
};
