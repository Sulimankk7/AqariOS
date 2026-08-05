import { ApiError } from '@/shared/lib/http';
import { FieldValues, UseFormSetError, Path } from 'react-hook-form';

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
  fallbackMessage: string = 'An unexpected error occurred. Please try again.'
): string {
  // 1. Offline check
  if (typeof window !== 'undefined' && typeof navigator !== 'undefined' && !navigator.onLine) {
    return 'Network connection lost. Please check your internet connection and try again.';
  }

  // 2. ApiError (RFC 7807 ProblemDetails from http.ts)
  if (error instanceof ApiError) {
    // Priority: Backend business message (detail or title)
    if (error.detail && error.detail.trim().length > 0) {
      return error.detail;
    }
    if (error.title && error.title.trim().length > 0 && error.title !== 'Bad Request' && error.title !== 'Server Error') {
      return error.title;
    }

    // Status code fallbacks if no specific detail/title is present
    switch (error.status) {
      case 401:
        return 'Your session has expired. Please sign in again.';
      case 403:
        return 'You do not have permission to perform this action.';
      case 404:
        return 'The requested resource could not be found.';
      case 409:
        return 'A conflict occurred. The resource may have been modified or is in an incompatible state.';
      case 422:
        return 'Validation failed. Please review the highlighted fields.';
      case 429:
        return 'Too many requests. Please wait a moment and try again.';
      case 500:
      case 502:
      case 503:
      case 504:
        return 'A server error occurred. Please try again later or contact support if the issue persists.';
      default:
        break;
    }

    // Fallback to error message if present and safe
    if (error.message && !isRawTechnicalMessage(error.message)) {
      return error.message;
    }
  }

  // 3. Native Error object
  if (error instanceof Error) {
    if (error.name === 'AbortError' || error.message.toLowerCase().includes('timeout')) {
      return 'The request timed out. Please try again.';
    }
    if (error.message.toLowerCase().includes('failed to fetch') || error.message.toLowerCase().includes('network error')) {
      return 'Unable to communicate with the server. Please check your connection.';
    }
    if (!isRawTechnicalMessage(error.message)) {
      return error.message;
    }
  }

  return fallbackMessage;
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
    const messageStr = Array.isArray(messages) ? messages.join(' ') : String(messages);

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

/**
 * Checks if a string looks like a raw stack trace, exception class name, or technical dump.
 */
function isRawTechnicalMessage(msg: string): boolean {
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
