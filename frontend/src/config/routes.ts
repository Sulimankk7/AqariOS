/**
 * Application route constants.
 * Single source of truth for all client-side route paths.
 * Use these constants everywhere — never hardcode strings.
 */

export const ROUTES = {
  /** Root redirect target */
  root: "/",

  // ── Authentication ─────────────────────────────────────────────────────────
  auth: {
    /** /auth/login */
    login: "/auth/login",
    /** /auth/register */
    register: "/auth/register",
    /** /auth/otp — Phone OTP request */
    otp: "/auth/otp",
    /** /auth/verify — OTP verification */
    verify: "/auth/verify",
    /** /auth/forgot-password */
    forgotPassword: "/auth/forgot-password",
    /** /auth/reset-password */
    resetPassword: "/auth/reset-password",
  },

  // ── Protected Application ──────────────────────────────────────────────────

  /** /dashboard */
  dashboard: { root: "/dashboard" },

  // Assets / Property
  /** /buildings */
  buildings: { root: "/buildings" },
  /** /apartments */
  apartments: { root: "/apartments" },
  /** /parking */
  parking: { root: "/parking" },

  // Leasing
  /** /leases */
  leases: { root: "/leases" },
  /** /tenants */
  tenants: { root: "/tenants" },

  // Finance
  /** /payments */
  payments: { root: "/payments" },
  /** /financial-operations */
  financialOperations: { root: "/financial-operations" },

  // Operations
  /** /maintenance */
  maintenance: { root: "/maintenance" },
  /** /marketplace */
  marketplace: { root: "/marketplace" },
  /** /documents */
  documents: { root: "/documents" },

  // System
  /** /notifications */
  notifications: { root: "/notifications" },
  /** /settings */
  settings: { root: "/settings" },
  /** /profile */
  profile: { root: "/profile" },
  /** /preferences */
  preferences: { root: "/preferences" },
} as const;

export type AppRoute = (typeof ROUTES)[keyof typeof ROUTES];
