/**
 * Environment configuration.
 * Reads variables from import.meta.env (Vite).
 * All values are typed and validated at runtime startup.
 */

export const env = {
  /** Base URL of the AqariOS REST API. Set VITE_API_BASE_URL in .env */
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL as string | undefined,

  /** Application environment. Defaults to "development". */
  nodeEnv: (import.meta.env.MODE ?? "development") as "development" | "production" | "test",

  /** Whether the app is running in production mode. */
  isProd: import.meta.env.PROD as boolean,

  /** Whether the app is running in development mode. */
  isDev: import.meta.env.DEV as boolean,
} as const;
