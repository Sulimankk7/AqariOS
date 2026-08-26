import { http } from '@/shared/lib/http';
import { apiUrl } from '@/config/api';
import { storage, STORAGE_KEYS } from '@/shared/services/storage';
import type {
  ExpenseDetailDto,
  ExpenseDto,
  ExpenseFilterParams,
  RentPaymentDetailDto,
  RentPaymentDto,
  RentPaymentFilterParams,
  RemindRentPaymentResponseDto,
} from '../types/financials.types';
import type { RentPaymentReceiptDto } from '@/features/payments/types/payments.types';

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

  getPaymentDetails: async (paymentId: string): Promise<RentPaymentDetailDto> => {
    return await http.get<RentPaymentDetailDto>(`/api/v1/rent-payments/${paymentId}`);
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

  downloadSettlementStatement: async (paymentId: string): Promise<Blob> => {
    const token = storage.get<string>(STORAGE_KEYS.accessToken);
    const headers: Record<string, string> = { Accept: 'application/pdf, application/problem+json' };
    if (token) headers.Authorization = `Bearer ${token}`;

    const response = await fetch(apiUrl(`/api/v1/rent-payments/${paymentId}/settlement-statement`), {
      method: 'GET',
      headers,
      credentials: 'include',
    });
    if (!response.ok) throw new Error(`Settlement statement download failed (${response.status})`);
    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('json') || contentType.includes('problem+json')) {
      throw new Error('Settlement statement response was not a PDF');
    }
    return response.blob();
  },

  getExpenses: async (params: ExpenseFilterParams = {}): Promise<ExpenseDto[]> => {
    const query = new URLSearchParams();
    if (params.buildingId) query.append('buildingId', params.buildingId);
    if (params.category !== undefined && params.category !== null && params.category !== '') {
      query.append('category', String(params.category));
    }
    if (params.dateFrom) query.append('dateFrom', params.dateFrom);
    if (params.dateTo) query.append('dateTo', params.dateTo);
    if (params.lastSeenId) query.append('lastSeenId', params.lastSeenId);
    if (params.lastSeenExpenseDate) query.append('lastSeenExpenseDate', params.lastSeenExpenseDate);
    if (params.pageSize) query.append('pageSize', String(params.pageSize));

    const qs = query.toString();
    return http.get<ExpenseDto[]>(`/api/v1/expenses${qs ? `?${qs}` : ''}`);
  },

  getExpenseDetails: async (expenseId: string): Promise<ExpenseDetailDto> =>
    http.get<ExpenseDetailDto>(`/api/v1/expenses/${expenseId}`),
};
