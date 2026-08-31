import {
  AllocationStatus,
  DueDateStatus,
  ExpenseCategory,
  ExpensePaymentMethod,
  PaymentMethod,
  PaymentPurpose,
} from '../types/financials.types';
import { getLocale, getRuntimeLanguage } from '@/shared/i18n';

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
 * Financial presentation follows the active application locale; API values remain unchanged.
 */
export function formatFinancialNumber(
  value: number,
  options?: Intl.NumberFormatOptions,
): string {
  return new Intl.NumberFormat(getLocale(getRuntimeLanguage()), options).format(value);
}

export function formatFinancialCurrency(
  value: number,
  currency = 'JOD',
  language: 'ar' | 'en' | string = 'ar',
): string {
  return new Intl.NumberFormat(getLocale(language === 'en' ? 'en' : 'ar'), {
    style: 'currency',
    currency: currency.toUpperCase(),
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatFinancialDate(value: Date | string | number | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return new Intl.DateTimeFormat(getLocale(getRuntimeLanguage()), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}
