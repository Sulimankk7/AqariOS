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
  EMAIL_ALREADY_EXISTS: 'errors.codes.EMAIL_ALREADY_EXISTS',
  PHONE_ALREADY_EXISTS: 'errors.codes.PHONE_ALREADY_EXISTS',
  TENANT_NATIONAL_ID_ALREADY_EXISTS: 'errors.codes.TENANT_NATIONAL_ID_ALREADY_EXISTS',
  UTILITY_ACCOUNT_ALREADY_LINKED: 'errors.codes.UTILITY_ACCOUNT_ALREADY_LINKED',
  RESOURCE_ALREADY_EXISTS: 'errors.codes.RESOURCE_ALREADY_EXISTS',
  USED_PLAN_IMMUTABLE: 'errors.codes.USED_PLAN_IMMUTABLE',
  PLAN_CHANGE_REQUEST_IMMUTABLE: 'errors.codes.PLAN_CHANGE_REQUEST_IMMUTABLE',
  INTERNAL_ERROR: 'errors.serverError',
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
  504: 'errors.timeout',
};

function isTimeoutMessage(message: string): boolean {
  return message.includes('timeout') || message.includes('timed out') || message.includes('aborted');
}

function isNetworkMessage(message: string): boolean {
  return message.includes('failed to fetch')
    || message.includes('network error')
    || message.includes('networkerror')
    || message === 'load failed'
    || message.includes('connection failed');
}

export type UserFacingErrorKind = 'validation' | 'unauthorized' | 'forbidden' | 'notFound' | 'conflict' | 'rateLimited' | 'server' | 'network' | 'timeout' | 'unknown';

/** Classifies errors for page state selection without exposing server details. */
export function getUserFacingErrorKind(error: unknown): UserFacingErrorKind {
  if (typeof window !== 'undefined' && typeof navigator !== 'undefined' && !navigator.onLine) return 'network';
  const message = error instanceof Error ? error.message.toLowerCase() : '';
  if (error instanceof ApiError) {
    if (isTimeoutMessage(message)) return 'timeout';
    if (isNetworkMessage(message)) return 'network';
    if (error.status === 400 || error.status === 422) return 'validation';
    if (error.status === 401) return 'unauthorized';
    if (error.status === 403) return 'forbidden';
    if (error.status === 404) return 'notFound';
    if (error.status === 409) return 'conflict';
    if (error.status === 429) return 'rateLimited';
    if (error.status >= 500) return 'server';
  }
  if (error instanceof Error && (error.name === 'AbortError' || isTimeoutMessage(message))) return 'timeout';
  if (error instanceof Error && isNetworkMessage(message)) return 'network';
  return 'unknown';
}

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

  const language = getRuntimeLanguage();
  const hasArabic = /[\u0600-\u06ff]/.test(fallback);
  if ((language === 'ar' && !hasArabic) || (language === 'en' && hasArabic)) {
    return generic;
  }

  return fallback;
}

const userSafeDetailStatuses = new Set([400, 404, 409, 422]);
const nonActionableProblemDetails = new Set([
  'one or more validation errors occurred.',
  'an unexpected error occurred.',
  'a database error occurred while processing the request.',
]);

/**
 * The API contract uses detail for domain/validation messages on these statuses.
 * Authentication, authorization, rate-limit and server failures stay localized.
 */
export function getSafeBackendDetail(error: ApiError): string | undefined {
  if (!userSafeDetailStatuses.has(error.status) || !error.detail) return undefined;
  if (error.code === 'INTERNAL_ERROR') return undefined;

  const detail = error.detail.trim();
  if (!detail || detail.length > 500 || nonActionableProblemDetails.has(detail.toLowerCase())) return undefined;
  if (isRawTechnicalMessage(detail)) return undefined;
  return detail;
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
    if (isTimeoutMessage(normalizedMessage)) {
      return translateCurrent('errors.timeout');
    }
    if (isNetworkMessage(normalizedMessage)) {
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
    // An unrecognized stable code must not fall through to an English backend
    // sentence. Feature callers can supply a localized contextual fallback.
    if (error.code) return safeFallback(fallbackMessage);
    const safeDetail = getSafeBackendDetail(error);
    if (safeDetail) return safeDetail;
    const statusKey = statusErrorKeys[error.status];
    if (statusKey) return translateCurrent(statusKey);
  }

  // 3. Native Error object
  if (error instanceof Error) {
    if (error.name === 'AbortError' || isTimeoutMessage(error.message.toLowerCase())) {
      return translateCurrent('errors.timeout');
    }
    if (isNetworkMessage(error.message.toLowerCase())) {
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
  return /(?:\b(?:system|microsoft|npgsql|dbcontext|stack\s*trace|innerexception|exception|axioserror)\b|(?:type|reference)error:|\bsqlstate\b|\bconnection\s*string\b|https?:\/\/|[a-z]:\\|\/(?:src|app|home|var)\/|(?:^|\n)\s*at\s+\S+|\b(?:post|get|put|patch|delete)\s+\/)/i.test(msg);
}
