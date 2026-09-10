/**
 * Shared HTTP Layer.
 * Wraps browser fetch with an Axios-like API structure:
 *   - Base URL prepending from config
 *   - Automatic JSON request/response handling
 *   - Request and response interceptor pipeline
 *   - Automatic Bearer token injection from storage
 *   - credentials: "include" enabled for HttpOnly cookie exchange (Axios withCredentials: true equivalent)
 *   - Global error normalization (RFC 7807 ProblemDetails & ValidationProblemDetails)
 *   - Production-Grade 401 Refresh Token & Mutex-Locked Session Invalidation Pipeline
 */

import { apiConfig } from "@/config/api";
import { storage, STORAGE_KEYS } from "@/shared/services/storage";
import { logger } from "@/shared/services/logger";
import { toast } from "sonner";
import { translateCurrent } from "@/shared/i18n";

// ── 401 Unauthorized & Refresh Token Pipeline State ─────────────────────────

type UnauthorizedHandler = (returnUrl?: string) => void;
let unauthorizedHandler: UnauthorizedHandler | null = null;

let isRefreshingToken = false;
let failedQueue: Array<{
  resolve: (token: string | null) => void;
  reject: (err: any) => void;
}> = [];

let hasShownSessionExpiredToast = false;

export function registerUnauthorizedHandler(handler: UnauthorizedHandler): void {
  unauthorizedHandler = handler;
}

export function resetUnauthorizedState(): void {
  hasShownSessionExpiredToast = false;
  isRefreshingToken = false;
  failedQueue = [];
}

const PUBLIC_AUTH_PATHS = [
  "/api/v1/auth/login",
  "/api/v1/auth/register",
  "/api/v1/auth/refresh",
  "/api/v1/auth/otp/request",
  "/api/v1/auth/otp/verify",
  "/api/v1/auth/password-reset/request",
  "/api/v1/auth/password-reset/verify-otp",
  "/api/v1/auth/password-reset/complete",
];

const PUBLIC_UI_ROUTES = [
  "/auth/login",
  "/auth/register",
  "/auth/forgot-password",
  "/auth/reset-password",
  "/auth/password-reset/verify",
  "/auth/otp",
];

function isPublicAuthEndpoint(url: string, skipAuth?: boolean): boolean {
  if (skipAuth) return true;
  return PUBLIC_AUTH_PATHS.some((path) => url.includes(path));
}

function isPublicUiRoute(): boolean {
  const currentPath = window.location.pathname.toLowerCase();
  return PUBLIC_UI_ROUTES.some((route) => currentPath.startsWith(route));
}

function processQueue(error: any, token: string | null = null) {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
}

function triggerSessionInvalidation(): void {
  // Show only ONE friendly toast notification per session expiry
  if (!hasShownSessionExpiredToast) {
    hasShownSessionExpiredToast = true;
    toast.error(translateCurrent("errors.sessionExpired"));
  }

  if (unauthorizedHandler) {
    try {
      unauthorizedHandler();
    } catch {
      // Fail-safe
    }
  }
}

// ── Error Classes & Types ─────────────────────────────────────────────────────

export interface ProblemDetailsPayload {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  code?: string;
  [key: string]: unknown;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly title?: string;
  public readonly detail?: string;
  public readonly validationErrors?: Record<string, string[]>;
  public readonly rawPayload?: unknown;
  public readonly code?: string;
  public readonly retryAfterSeconds?: number;

  constructor(status: number, message: string, payload?: ProblemDetailsPayload, retryAfterSeconds?: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.title = payload?.title;
    this.detail = payload?.detail;
    this.validationErrors = payload?.errors;
    this.rawPayload = payload;
    this.code = payload?.code;
    this.retryAfterSeconds = retryAfterSeconds;
  }
}

// ── Interceptor Infrastructure ────────────────────────────────────────────────

export interface RequestConfig extends Omit<RequestInit, "body"> {
  url: string;
  body?: unknown;
  headers?: Record<string, string>;
  skipAuth?: boolean;
  isRetry?: boolean;
}

type RequestInterceptor = (config: RequestConfig) => RequestConfig | Promise<RequestConfig>;
type ResponseInterceptor = (response: Response) => Response | Promise<Response>;
type ErrorInterceptor = (error: ApiError) => Promise<never>;

class InterceptorManager<T extends (value: any) => any> {
  private handlers: Array<T | null> = [];

  use(handler: T): number {
    this.handlers.push(handler);
    return this.handlers.length - 1;
  }

  eject(id: number): void {
    if (this.handlers[id]) {
      this.handlers[id] = null;
    }
  }

  async run(initial: any): Promise<any> {
    let current = initial;
    for (const handler of this.handlers) {
      if (handler) {
        current = await handler(current);
      }
    }
    return current;
  }
}

// ── HttpClient Implementation ─────────────────────────────────────────────────

export class HttpClient {
  public readonly interceptors = {
    request: new InterceptorManager<RequestInterceptor>(),
    response: new InterceptorManager<ResponseInterceptor>(),
    error: new InterceptorManager<ErrorInterceptor>(),
  };

  constructor() {
    // Register default Bearer Token Injection Request Interceptor
    this.interceptors.request.use((config) => {
      if (!config.skipAuth) {
        const token = storage.get<string>(STORAGE_KEYS.accessToken);
        if (token) {
          config.headers = {
            ...(config.headers ?? {}),
            Authorization: `Bearer ${token}`,
          };
        }
      }
      return config;
    });
  }

  /** Normalises relative or full URLs against apiConfig.baseUrl */
  private resolveUrl(path: string): string {
    if (path.startsWith("http://") || path.startsWith("https://")) {
      return path;
    }
    const base = apiConfig.baseUrl.replace(/\/$/, "");
    const normalised = path.startsWith("/") ? path : `/${path}`;
    return `${base}${normalised}`;
  }

  /** Attempt single silent refresh request using HttpOnly cookie or refresh token */
  private async performTokenRefresh(): Promise<string | null> {
    try {
      const fullUrl = this.resolveUrl("/api/v1/auth/refresh");
      const res = await fetch(fullUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({}),
        credentials: "include",
      });

      if (!res.ok) {
        return null;
      }

      const data = await res.json();
      if (data && data.accessToken) {
        storage.setAccessToken(data.accessToken, data.isPersistentSession === true);
        return data.accessToken;
      }
      return null;
    } catch {
      return null;
    }
  }

  /** Low-level request dispatcher */
  public async request<T>(config: RequestConfig): Promise<T> {
    let finalConfig = await this.interceptors.request.run(config);
    const fullUrl = this.resolveUrl(finalConfig.url);

    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      Accept: "application/json, application/problem+json",
      ...(finalConfig.headers ?? {}),
    };

    let fetchBody: string | undefined = undefined;
    if (finalConfig.body !== undefined) {
      fetchBody = typeof finalConfig.body === "string" ? finalConfig.body : JSON.stringify(finalConfig.body);
    }

    try {
      let response = await fetch(fullUrl, {
        ...finalConfig,
        headers,
        body: fetchBody,
        credentials: "include",
      });

      response = await this.interceptors.response.run(response);

      // Handle HTTP 401 Unauthorized
      if (response.status === 401) {
        const isPublic = isPublicAuthEndpoint(finalConfig.url, finalConfig.skipAuth);

        // Requirement 4: 401 on public endpoints must NOT trigger session invalidation or logout
        if (isPublic) {
          logger.warn(`401 on public endpoint ${finalConfig.url} — bypassing session invalidation.`);
        } else if (finalConfig.isRetry) {
          // Requirement 1 & 8: Request already retried once — prevent infinite retry loops
          logger.warn(`Retry failed with 401 at ${finalConfig.url}. Triggering session invalidation.`);
          triggerSessionInvalidation();
        } else {
          // Requirement 1 & 2: Mutex-locked single Refresh Token flow for multiple simultaneous 401s
          if (isRefreshingToken) {
            return new Promise<T>((resolve, reject) => {
              failedQueue.push({
                resolve: (newToken: string | null) => {
                  if (newToken) {
                    finalConfig.headers = {
                      ...(finalConfig.headers ?? {}),
                      Authorization: `Bearer ${newToken}`,
                    };
                    finalConfig.isRetry = true;
                    this.request<T>(finalConfig).then(resolve).catch(reject);
                  } else {
                    reject(new ApiError(401, "Session expired"));
                  }
                },
                reject: (err: any) => reject(err),
              });
            });
          }

          isRefreshingToken = true;
          logger.info(`401 encountered at ${finalConfig.url}. Attempting single token refresh...`);

          const newAccessToken = await this.performTokenRefresh();
          isRefreshingToken = false;

          if (newAccessToken) {
            logger.info("Token refresh succeeded. Retrying original request...");
            processQueue(null, newAccessToken);
            finalConfig.headers = {
              ...(finalConfig.headers ?? {}),
              Authorization: `Bearer ${newAccessToken}`,
            };
            finalConfig.isRetry = true;
            return this.request<T>(finalConfig);
          } else {
            logger.warn("Token refresh failed. Invalidating session.");
            processQueue(new ApiError(401, "Session expired"), null);
            triggerSessionInvalidation();
          }
        }
      }

      if (!response.ok) {
        let payload: ProblemDetailsPayload | undefined;
        try {
          const contentType = response.headers.get("content-type") ?? "";
          if (contentType.includes("json") || contentType.includes("problem+json")) {
            payload = (await response.json()) as ProblemDetailsPayload;
          }
        } catch {
          // Response body was not JSON
        }

        const errorMessage =
          payload?.detail ??
          payload?.title ??
          (payload?.errors ? Object.values(payload.errors).flat().join(" ") : null) ??
          `HTTP ${response.status}: ${response.statusText}`;

        const retryAfter = Number.parseInt(response.headers.get("Retry-After") ?? "", 10);
        const apiError = new ApiError(response.status, errorMessage, payload,
          Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : undefined);
        logger.error(`API Error [${response.status}] ${finalConfig.url}`, apiError);
        throw apiError;
      }

      if (response.status === 204) {
        return undefined as T;
      }

      const contentType = response.headers.get("content-type") ?? "";
      if (contentType.includes("json")) {
        return (await response.json()) as T;
      }

      return (await response.text()) as unknown as T;
    } catch (err) {
      if (err instanceof ApiError) {
        await this.interceptors.error.run(err).catch(() => {});
        throw err;
      }
      const genericError = new ApiError(500, err instanceof Error ? err.message : "Network request failed");
      logger.error(`Network Error ${finalConfig.url}`, genericError);
      throw genericError;
    }
  }

  public get<T>(url: string, config?: Omit<RequestConfig, "url" | "method">): Promise<T> {
    return this.request<T>({ ...config, url, method: "GET" });
  }

  public post<T>(url: string, body?: unknown, config?: Omit<RequestConfig, "url" | "method" | "body">): Promise<T> {
    return this.request<T>({ ...config, url, method: "POST", body });
  }

  public put<T>(url: string, body?: unknown, config?: Omit<RequestConfig, "url" | "method" | "body">): Promise<T> {
    return this.request<T>({ ...config, url, method: "PUT", body });
  }

  public patch<T>(url: string, body?: unknown, config?: Omit<RequestConfig, "url" | "method" | "body">): Promise<T> {
    return this.request<T>({ ...config, url, method: "PATCH", body });
  }

  public delete<T>(url: string, config?: Omit<RequestConfig, "url" | "method">): Promise<T> {
    return this.request<T>({ ...config, url, method: "DELETE" });
  }
}

/** Global shared instance of HttpClient */
export const http = new HttpClient();
