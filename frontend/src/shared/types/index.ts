/**
 * Shared application-wide TypeScript types.
 * Feature-specific types live in their own feature/types/ folders.
 */

/** ISO 639-1 language codes supported by AqariOS. */
export type SupportedLanguage = "en" | "ar";

/** Pagination metadata returned by list endpoints. */
export interface PaginationMeta {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

/** Generic paginated API response wrapper. */
export interface PaginatedResponse<T> {
  data: T[];
  meta: PaginationMeta;
}

/** Generic API success response wrapper. */
export interface ApiSuccessResponse<T = void> {
  success: true;
  data: T;
  message?: string;
}

/** Generic API error response shape returned by the backend. */
export interface ApiErrorResponse {
  success: false;
  message: string;
  errors?: Record<string, string[]>;
}

/** Union of API response shapes. */
export type ApiResponse<T = void> = ApiSuccessResponse<T> | ApiErrorResponse;
