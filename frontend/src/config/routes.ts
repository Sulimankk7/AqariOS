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

  // Assets / Property Hierarchy
  /** /buildings */
  buildings: {
    root: "/buildings",
    new: "/buildings/new",
    details: (id: string) => `/buildings/${id}`,
    edit: (id: string) => `/buildings/${id}/edit`,
    floors: {
      new: (buildingId: string) => `/buildings/${buildingId}/floors/new`,
      details: (buildingId: string, floorId: string) => `/buildings/${buildingId}/floors/${floorId}`,
      edit: (buildingId: string, floorId: string) => `/buildings/${buildingId}/floors/${floorId}/edit`,
    },
  },
  /** /apartments */
  apartments: { 
    root: "/apartments",
    new: "/apartments/new",
    floorNew: (buildingId: string, floorId: string) => `/buildings/${buildingId}/floors/${floorId}/apartments/new`,
    details: (id: string) => `/apartments/${id}`,
    edit: (id: string) => `/apartments/${id}/edit`,
  },
  /** /parking */
  parking: { root: "/parking" },

  // Leasing
  /** /leases */
  leases: {
    root: "/leases",
    new: "/leases/new",
    details: (id: string) => `/leases/${id}`,
    edit: (id: string) => `/leases/${id}/edit`,
  },
  /** /tenants */
  tenants: {
    root: "/tenants",
    new: "/tenants/new",
    details: (id: string) => `/tenants/${id}`,
    edit: (id: string) => `/tenants/${id}/edit`,
  },

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

  // Tenant Portal Self-Service
  tenant: {
    dashboard: "/tenant/dashboard",
    profile: "/tenant/profile",
    lease: "/tenant/lease",
    payments: "/tenant/payments",
  },
} as const;

export type AppRoute = (typeof ROUTES)[keyof typeof ROUTES];
