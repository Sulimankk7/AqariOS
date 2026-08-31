import { ApiError } from '@/shared/lib/http';
import { FieldValues, UseFormSetError, Path } from 'react-hook-form';
import { getRuntimeLanguage, translateCurrent } from '@/shared/i18n';

const apiErrorCodeKeys: Record<string, string> = {
  TenantNotFound: 'errors.codes.TenantNotFound',
  InvalidLeaseStatus: 'errors.codes.InvalidLeaseStatus',
  PaymentAlreadyProcessed: 'errors.codes.PaymentAlreadyProcessed',
  CONCURRENCY_CONFLICT: 'errors.codes.CONCURRENCY_CONFLICT',
  CURRENT_SUBSCRIPTION_ALREADY_EXISTS: 'errors.codes.CURRENT_SUBSCRIPTION_ALREADY_EXISTS',
  PENDING_PLAN_CHANGE_ALREADY_EXISTS: 'errors.codes.PENDING_PLAN_CHANGE_ALREADY_EXISTS',
  PLAN_CHANGE_INVALID_LIFECYCLE_TRANSITION: 'errors.codes.PLAN_CHANGE_INVALID_LIFECYCLE_TRANSITION',
  PLAN_CODE_ALREADY_EXISTS: 'errors.codes.PLAN_CODE_ALREADY_EXISTS',
  TENANT_PHONE_ALREADY_EXISTS: 'errors.codes.TENANT_PHONE_ALREADY_EXISTS',
  OTP_ACCOUNT_NOT_FOUND: 'errors.codes.OTP_ACCOUNT_NOT_FOUND',
  PASSWORD_RESET_TOKEN_INVALID: 'errors.codes.PASSWORD_RESET_TOKEN_INVALID',
  PASSWORD_RESET_TOKEN_EXPIRED: 'errors.codes.PASSWORD_RESET_TOKEN_EXPIRED',
  PASSWORD_RESET_TOKEN_USED: 'errors.codes.PASSWORD_RESET_TOKEN_USED',
  PASSWORD_RESET_OTP_INVALID: 'errors.codes.PASSWORD_RESET_OTP_INVALID',
  PASSWORD_RESET_OTP_EXPIRED: 'errors.codes.PASSWORD_RESET_OTP_EXPIRED',
  PASSWORD_RESET_OTP_ATTEMPTS_EXCEEDED: 'errors.codes.PASSWORD_RESET_OTP_ATTEMPTS_EXCEEDED',
  PASSWORD_RESET_AUTHORIZATION_EXPIRED: 'errors.codes.PASSWORD_RESET_AUTHORIZATION_EXPIRED',
};

const statusErrorKeys: Record<number, string> = {
  400: 'errors.validation',
  401: 'errors.sessionExpired',
  403: 'errors.forbidden',
  404: 'errors.notFound',
  409: 'errors.conflict',
  422: 'errors.validation',
  429: 'errors.tooManyRequests',
  500: 'errors.serverError',
  502: 'errors.serviceUnavailable',
  503: 'errors.serviceUnavailable',
  504: 'errors.serviceUnavailable',
};

function backendTranslationKey(value: string | undefined): string | undefined {
  if (!value) return undefined;
  const trimmed = value.trim();
  if (/^(errors|validation)\.[a-zA-Z0-9_.-]+$/.test(trimmed)) return trimmed;
  return apiErrorCodeKeys[trimmed];
}

function safeFallback(fallbackMessage: string): string {
  const fallback = fallbackMessage.trim();
  const generic = translateCurrent('errors.generic');
  const unavailableMessages = new Set([
    translateCurrent('common.missingTranslation'),
    translateCurrent('common.unknown'),
  ]);

  if (!fallback || unavailableMessages.has(fallback) || isRawTechnicalMessage(fallback)) {
    return generic;
  }

  if (getRuntimeLanguage() === 'ar' && !/[\u0600-\u06ff]/.test(fallback)) {
    return generic;
  }

  return fallback;
}

/**
 * Extracts a clean, human-readable error message from any error object.
 * Priority:
 * 1. Offline network check
 * 2. Backend ProblemDetails detail/title
 * 3. Specific HTTP status fallbacks (401, 403, 404, 409, 429, 500)
 * 4. Exception message (sanitized)
 * 5. Fallback message
 */
export function extractUserFriendlyError(
  error: unknown,
  fallbackMessage: string = translateCurrent('errors.generic')
): string {
  // 1. Offline check
  if (typeof window !== 'undefined' && typeof navigator !== 'undefined' && !navigator.onLine) {
    return translateCurrent('errors.network');
  }

  // 2. ApiError (RFC 7807 ProblemDetails from http.ts)
  if (error instanceof ApiError) {
    const normalizedMessage = error.message.toLowerCase();
    if (normalizedMessage.includes('timeout') || normalizedMessage.includes('aborted')) {
      return translateCurrent('errors.timeout');
    }
    if (normalizedMessage.includes('failed to fetch') || normalizedMessage.includes('network')) {
      return translateCurrent('errors.network');
    }
    const codeKey = error.code ? apiErrorCodeKeys[error.code] : undefined;
    if (codeKey) return translateCurrent(codeKey);
    const detailKey = backendTranslationKey(error.detail);
    if (detailKey) return translateCurrent(detailKey);
    const titleKey = backendTranslationKey(error.title);
    if (titleKey) return translateCurrent(titleKey);
    if (error.status === 429 && error.retryAfterSeconds) {
      return translateCurrent('errors.tooManyRequestsRetryAfter', { seconds: error.retryAfterSeconds });
    }
    const statusKey = statusErrorKeys[error.status];
    if (statusKey) return translateCurrent(statusKey);
  }

  // 3. Native Error object
  if (error instanceof Error) {
    if (error.name === 'AbortError' || error.message.toLowerCase().includes('timeout')) {
      return translateCurrent('errors.timeout');
    }
    if (error.message.toLowerCase().includes('failed to fetch') || error.message.toLowerCase().includes('network error')) {
      return translateCurrent('errors.network');
    }
  }

  return safeFallback(fallbackMessage);
}

/**
 * Maps ASP.NET Core ValidationProblemDetails (errors object) directly to React Hook Form fields.
 * Returns true if at least one field error was set, false otherwise.
 */
export function mapApiValidationErrors<TFieldValues extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<TFieldValues>
): boolean {
  if (!(error instanceof ApiError) || !error.validationErrors) {
    return false;
  }

  const entries = Object.entries(error.validationErrors);
  if (entries.length === 0) {
    return false;
  }

  let mappedAny = false;

  for (const [rawKey, messages] of entries) {
    if (!messages || messages.length === 0) continue;

    // Convert PascalCase backend keys (e.g., "ContractNumber", "StartDate") to camelCase ("contractNumber", "startDate")
    const camelKey = rawKey.charAt(0).toLowerCase() + rawKey.slice(1);
    const messageStr = localizeValidationMessages(messages);

    try {
      setError(camelKey as Path<TFieldValues>, {
        type: 'server',
        message: messageStr,
      });
      mappedAny = true;
    } catch {
      // Key doesn't exist on form schema
    }
  }

  return mappedAny;
}

function localizeValidationMessages(messages: string[] | string): string {
  const values = Array.isArray(messages) ? messages : [String(messages)];
  const keys = values.map((message) => {
    const normalized = message.trim().toLowerCase();
    if (normalized.includes('phone') && normalized.includes('e.164')) return 'validation.tenantPhoneE164';
    if (normalized.includes('required')) return 'validation.required';
    if (normalized.includes('email')) return 'validation.invalidEmail';
    if (normalized.includes('phone')) return 'validation.invalidPhone';
    if (normalized.includes('date')) return 'validation.invalidDate';
    if (normalized.includes('amount') || normalized.includes('greater than zero')) return 'validation.invalidAmount';
    if (normalized.includes('valid') || normalized.includes('select')) return 'validation.invalidSelection';
    return 'validation.serverField';
  });
  return [...new Set(keys)].map((key) => translateCurrent(key)).join(' ');
}

export function localizeValidationMessage(message: unknown): string {
  const value = String(message ?? '').trim();
  if (!value) return '';
  if (/^validation\.[a-zA-Z0-9_.-]+$/.test(value)) return translateCurrent(value);
  const knownLocalizedMessages = [
    'validation.required',
    'validation.invalidEmail',
    'validation.invalidPhone',
    'validation.tenantPhoneE164',
    'validation.invalidDate',
    'validation.invalidAmount',
    'validation.passwordMismatch',
    'validation.passwordPolicy',
    'validation.invalidSelection',
    'validation.serverField',
  ].map((key) => translateCurrent(key));
  if (knownLocalizedMessages.includes(value)) return value;
  const normalized = value.toLowerCase();
  if (normalized.includes('phone') && normalized.includes('e.164')) return translateCurrent('validation.tenantPhoneE164');
  if (normalized.includes('required')) return translateCurrent('validation.required');
  if (normalized.includes('email')) return translateCurrent('validation.invalidEmail');
  if (normalized.includes('phone')) return translateCurrent('validation.invalidPhone');
  if (normalized.includes('password') && (normalized.includes('match') || normalized.includes('same'))) return translateCurrent('validation.passwordMismatch');
  if (normalized.includes('password') && (normalized.includes('uppercase') || normalized.includes('lowercase') || normalized.includes('digit') || normalized.includes('numeric'))) return translateCurrent('validation.passwordPolicy');
  if (normalized.includes('date')) return translateCurrent('validation.invalidDate');
  if (normalized.includes('amount') || normalized.includes('greater than zero') || normalized.includes('negative')) return translateCurrent('validation.invalidAmount');
  if (normalized.includes('select') || normalized.includes('valid') || normalized.includes('uuid')) return translateCurrent('validation.invalidSelection');
  if (normalized.includes('at least')) return translateCurrent('validation.minLength', { min: value.match(/\d+/)?.[0] ?? '' });
  if (normalized.includes('exceed') || normalized.includes('max') || normalized.includes('too long')) return translateCurrent('validation.maxLength', { max: value.match(/\d+/)?.[0] ?? '' });
  return translateCurrent('validation.serverField');
}

/**
 * Checks if a string looks like a raw stack trace, exception class name, or technical dump.
 */
export function isRawTechnicalMessage(msg: string): boolean {
  if (!msg) return true;
  const technicalIndicators = [
    'System.',
    'Exception',
    'NullReferenceException',
    'InvalidOperationException',
    'AxiosError',
    'TypeError:',
    'ReferenceError:',
    'at ',
    'http://',
    'https://',
    'POST ',
    'GET ',
    'PUT ',
    'DELETE ',
  ];
  return technicalIndicators.some((indicator) => msg.includes(indicator));
}
