/**
 * Shared HTTP Layer.
 * Wraps browser fetch with an Axios-like API structure:
 *   - Base URL prepending from config
 *   - Automatic JSON request/response handling
 *   - Request and response interceptor pipeline
 *   - Automatic Bearer token injection from storage
 *   - credentials: "include" enabled for HttpOnly cookie exchange (Axios withCredentials: true equivalent)
 *   - Global error normalization (RFC 7807 ProblemDetails & ValidationProblemDetails)
 */

import { apiConfig } from "@/config/api";
import { storage, STORAGE_KEYS } from "@/shared/services/storage";
import { logger } from "@/shared/services/logger";

// ── Error Classes & Types ─────────────────────────────────────────────────────

export interface ProblemDetailsPayload {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly title?: string;
  public readonly detail?: string;
  public readonly validationErrors?: Record<string, string[]>;
  public readonly rawPayload?: unknown;

  constructor(status: number, message: string, payload?: ProblemDetailsPayload) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.title = payload?.title;
    this.detail = payload?.detail;
    this.validationErrors = payload?.errors;
    this.rawPayload = payload;
  }
}

// ── Interceptor Infrastructure ────────────────────────────────────────────────

export interface RequestConfig extends Omit<RequestInit, "body"> {
  url: string;
  body?: unknown;
  headers?: Record<string, string>;
  skipAuth?: boolean;
}

type RequestInterceptor = (config: RequestConfig) => RequestConfig | Promise<RequestConfig>;
type ResponseInterceptor = (response: Response) => Response | Promise<Response>;
type ErrorInterceptor = (error: ApiError) => Promise<never>;

class InterceptorManager<T> {
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
        // Enable withCredentials equivalent: send HttpOnly cookies with every request
        credentials: "include",
      });

      response = await this.interceptors.response.run(response);

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

        const apiError = new ApiError(response.status, errorMessage, payload);
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
