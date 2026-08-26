import {
  AllocationStatus,
  DueDateStatus,
  ExpenseCategory,
  ExpensePaymentMethod,
  PaymentMethod,
  PaymentPurpose,
} from '../types/financials.types';

type NumericEnum = Record<string, string | number>;

export function enumNumber(value: string | number | null | undefined, values: NumericEnum): number | null {
  if (value === null || value === undefined || value === '') return null;
  if (typeof value === 'number') return value;

  const numeric = Number(value);
  if (Number.isFinite(numeric)) return numeric;

  const normalized = value.replace(/[^a-z0-9]/gi, '').toLowerCase();
  const match = Object.entries(values).find(
    ([key, enumValue]) => typeof enumValue === 'number' && key.replace(/[^a-z0-9]/gi, '').toLowerCase() === normalized,
  );
  return typeof match?.[1] === 'number' ? match[1] : null;
}

export const paymentPurposeValue = (value: string | number | null | undefined) =>
  enumNumber(value, PaymentPurpose);

export const dueDateStatusValue = (value: string | number | null | undefined) =>
  enumNumber(value, DueDateStatus);

export const paymentMethodValue = (value: string | number | null | undefined) =>
  enumNumber(value, PaymentMethod);

export const allocationStatusValue = (value: string | number | null | undefined) =>
  enumNumber(value, AllocationStatus);

export const expenseCategoryValue = (value: string | number | null | undefined) =>
  enumNumber(value, ExpenseCategory);

export const expensePaymentMethodValue = (value: string | number | null | undefined) =>
  enumNumber(value, ExpensePaymentMethod);

export const isScheduledInstallment = (value: string | number | null | undefined) =>
  paymentPurposeValue(value) === PaymentPurpose.ScheduledInstallment;

export const isReceivedPayment = (value: string | number | null | undefined) =>
  paymentPurposeValue(value) === PaymentPurpose.UnallocatedReceipt;

/**
 * Financial Operations intentionally uses western digits in both UI languages.
 * These helpers affect presentation only; API values remain unchanged.
 */
export function formatFinancialNumber(
  value: number,
  options?: Intl.NumberFormatOptions,
): string {
  return new Intl.NumberFormat('en-US', options).format(value);
}

export function formatFinancialCurrency(
  value: number,
  currency = 'JOD',
  language: 'ar' | 'en' | string = 'ar',
): string {
  const amount = formatFinancialNumber(value, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  const currencyLabel = currency.toUpperCase() === 'JOD' && language === 'ar' ? 'د.أ' : currency.toUpperCase();
  return `${amount} ${currencyLabel}`;
}

export function formatFinancialDate(value: Date | string | number | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}
