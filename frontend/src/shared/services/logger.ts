/**
 * Application logger service.
 * Wraps console methods with environment-aware log levels.
 * In production, only warnings and errors are emitted.
 * Swap this implementation for an observability SDK (Sentry, Datadog, etc.)
 * without touching any call sites.
 */

import { env } from "@/config/env";

type LogLevel = "debug" | "info" | "warn" | "error";

type LogPayload = Record<string, unknown> | Error | unknown;

function shouldLog(level: LogLevel): boolean {
  if (env.isProd) {
    return false;
  }
  return true;
}

function format(level: LogLevel, message: string, payload?: LogPayload): void {
  if (!shouldLog(level)) return;

  const prefix = `[AqariOS] [${level.toUpperCase()}]`;

  switch (level) {
    case "debug":
      payload !== undefined
        ? console.debug(prefix, message, payload)
        : console.debug(prefix, message);
      break;
    case "info":
      payload !== undefined
        ? console.info(prefix, message, payload)
        : console.info(prefix, message);
      break;
    case "warn":
      payload !== undefined
        ? console.warn(prefix, message, payload)
        : console.warn(prefix, message);
      break;
    case "error":
      payload !== undefined
        ? console.error(prefix, message, payload)
        : console.error(prefix, message);
      break;
  }
}

export const logger = {
  debug: (message: string, payload?: LogPayload) => format("debug", message, payload),
  info:  (message: string, payload?: LogPayload) => format("info",  message, payload),
  warn:  (message: string, payload?: LogPayload) => format("warn",  message, payload),
  error: (message: string, payload?: LogPayload) => format("error", message, payload),
};
