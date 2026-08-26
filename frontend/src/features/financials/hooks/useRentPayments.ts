import { useQuery, useMutation } from '@tanstack/react-query';
import { financialsApi } from '../api/financials.api';
import type { ExpenseFilterParams, RentPaymentFilterParams, RemindRentPaymentResponseDto } from '../types/financials.types';
import { toast } from 'sonner';
import { useTranslation } from '@/shared/i18n';
import { ApiError } from '@/shared/lib/http';
import { extractUserFriendlyError } from '@/shared/utils';

export const financialKeys = {
  all: ['financials'] as const,
  rentPayments: () => [...financialKeys.all, 'rent-payments'] as const,
  rentPaymentsList: (params: RentPaymentFilterParams) => [...financialKeys.rentPayments(), params] as const,
  paymentDetails: (id: string | null) => [...financialKeys.all, 'payment-detail', id] as const,
  paymentReceipt: (id: string | null) => [...financialKeys.all, 'payment-receipt', id] as const,
  expenses: () => [...financialKeys.all, 'expenses'] as const,
  expensesList: (params: ExpenseFilterParams) => [...financialKeys.expenses(), params] as const,
  expenseDetails: (id: string | null) => [...financialKeys.expenses(), 'detail', id] as const,
};

export function useRentPayments(params: RentPaymentFilterParams = {}) {
  return useQuery({
    queryKey: financialKeys.rentPaymentsList(params),
    queryFn: () => financialsApi.getRentPayments(params),
    staleTime: 30_000, // 30 seconds
  });
}

export function useExpenses(params: ExpenseFilterParams = {}) {
  return useQuery({
    queryKey: financialKeys.expensesList(params),
    queryFn: () => financialsApi.getExpenses(params),
    staleTime: 30_000,
  });
}

export function useExpenseDetails(expenseId: string | null) {
  return useQuery({
    queryKey: financialKeys.expenseDetails(expenseId),
    queryFn: () => expenseId ? financialsApi.getExpenseDetails(expenseId) : Promise.reject('No expense ID'),
    enabled: !!expenseId,
    staleTime: 60_000,
  });
}

export function usePaymentDetails(paymentId: string | null) {
  return useQuery({
    queryKey: financialKeys.paymentDetails(paymentId),
    queryFn: () => (paymentId ? financialsApi.getPaymentDetails(paymentId) : Promise.reject('No payment ID')),
    enabled: !!paymentId,
    staleTime: 60_000,
  });
}

export function usePaymentReceipt(paymentId: string | null) {
  return useQuery({
    queryKey: financialKeys.paymentReceipt(paymentId),
    queryFn: () => (paymentId ? financialsApi.getReceiptByRentPaymentId(paymentId) : Promise.resolve(null)),
    enabled: !!paymentId,
    staleTime: 60_000,
  });
}

export function useRemindRentPayment() {
  const { t } = useTranslation();

  return useMutation<RemindRentPaymentResponseDto, unknown, string>({
    mutationFn: (paymentId: string) => financialsApi.remindTenant(paymentId),
    onSuccess: () => {
      toast.success(t('financials.notifySuccess'));
    },
    onError: (err: unknown) => {
      let message = t('financials.errorReminderFailed');
      if (err instanceof ApiError || (err && typeof err === 'object')) {
        const raw = (err as ApiError).rawPayload as Record<string, any> | undefined;
        const code = raw?.code || raw?.extensions?.code || (err as any)?.code;

        switch (code) {
          case 'TENANT_ACCOUNT_UNAVAILABLE':
            message = t('financials.errorTenantAccountUnavailable');
            break;
          case 'PAYMENT_ALREADY_PAID':
            message = t('financials.errorPaymentAlreadyPaid');
            break;
          case 'PAYMENT_CANCELLED':
            message = t('financials.errorPaymentCancelled');
            break;
          case 'PAYMENT_FULLY_SETTLED':
            message = t('financials.errorPaymentFullySettled');
            break;
          default:
            message = extractUserFriendlyError(err, t('financials.errorReminderFailed'));
            break;
        }
      } else {
        message = extractUserFriendlyError(err, t('financials.errorReminderFailed'));
      }
      toast.error(message);
    },
  });
}
