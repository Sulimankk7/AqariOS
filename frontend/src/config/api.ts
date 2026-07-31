/**
 * API configuration.
 * Centralises all base URL and timeout settings for HTTP requests.
 */

import { env } from "./env";

export const apiConfig = {
  /** Base host/URL used by the HTTP client for all API calls. */
  baseUrl: env.apiBaseUrl ?? "http://localhost:5235/",

  /** Default request timeout in milliseconds. */
  timeoutMs: 15_000,
} as const;

/**
 * Builds a full API URL for the given path.
 * @example apiUrl("/api/v1/auth/login") → "http://localhost:5000/api/v1/auth/login"
 */
export function apiUrl(path: string): string {
  const base = apiConfig.baseUrl.replace(/\/$/, "");
  const normalised = path.startsWith("/") ? path : `/${path}`;
  return `${base}${normalised}`;
}
