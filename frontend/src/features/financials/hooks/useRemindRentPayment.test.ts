import test from 'node:test';
import assert from 'node:assert/strict';
import { ApiError } from '@/shared/lib/http';

// Helper error resolver matching useRentPayments.ts error handling logic
function resolveReminderErrorMessage(err: unknown, t: (key: string) => string): string {
  if (err instanceof ApiError || (err && typeof err === 'object')) {
    const raw = (err as ApiError).rawPayload as Record<string, any> | undefined;
    const code = raw?.code || raw?.extensions?.code || (err as any)?.code;

    switch (code) {
      case 'TENANT_ACCOUNT_UNAVAILABLE':
        return t('financials.errorTenantAccountUnavailable');
      case 'PAYMENT_ALREADY_PAID':
        return t('financials.errorPaymentAlreadyPaid');
      case 'PAYMENT_CANCELLED':
        return t('financials.errorPaymentCancelled');
      case 'PAYMENT_FULLY_SETTLED':
        return t('financials.errorPaymentFullySettled');
      default:
        break;
    }
  }
  return t('financials.errorReminderFailed');
}

const mockTranslations: Record<string, string> = {
  'financials.notifySuccess': 'Payment reminder sent successfully.',
  'financials.errorTenantAccountUnavailable': 'The tenant has not activated their portal account yet.',
  'financials.errorPaymentAlreadyPaid': 'This payment has already been paid.',
  'financials.errorPaymentCancelled': 'This payment has been cancelled.',
  'financials.errorPaymentFullySettled': 'This payment has no outstanding balance.',
  'financials.errorReminderFailed': 'Failed to send payment reminder. Please try again later.',
};

const t = (key: string) => mockTranslations[key] || key;

test('resolveReminderErrorMessage maps TENANT_ACCOUNT_UNAVAILABLE correctly', () => {
  const error = new ApiError(422, 'Tenant account unavailable', { code: 'TENANT_ACCOUNT_UNAVAILABLE' });
  const message = resolveReminderErrorMessage(error, t);
  assert.equal(message, 'The tenant has not activated their portal account yet.');
});

test('resolveReminderErrorMessage maps PAYMENT_ALREADY_PAID correctly', () => {
  const error = new ApiError(422, 'Payment already paid', { code: 'PAYMENT_ALREADY_PAID' });
  const message = resolveReminderErrorMessage(error, t);
  assert.equal(message, 'This payment has already been paid.');
});

test('resolveReminderErrorMessage maps PAYMENT_CANCELLED correctly', () => {
  const error = new ApiError(422, 'Payment cancelled', { code: 'PAYMENT_CANCELLED' });
  const message = resolveReminderErrorMessage(error, t);
  assert.equal(message, 'This payment has been cancelled.');
});

test('resolveReminderErrorMessage maps PAYMENT_FULLY_SETTLED correctly', () => {
  const error = new ApiError(422, 'Payment fully settled', { code: 'PAYMENT_FULLY_SETTLED' });
  const message = resolveReminderErrorMessage(error, t);
  assert.equal(message, 'This payment has no outstanding balance.');
});

test('resolveReminderErrorMessage falls back gracefully on unknown 500 error', () => {
  const error = new ApiError(500, 'Internal Server Error');
  const message = resolveReminderErrorMessage(error, t);
  assert.equal(message, 'Failed to send payment reminder. Please try again later.');
});
