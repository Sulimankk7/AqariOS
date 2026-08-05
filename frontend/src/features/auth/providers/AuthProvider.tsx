/**
 * AuthProvider — Single Source of Truth for Authentication Architecture.
 *
 * Implements:
 * 1. Startup Session Validation: Validates tokens via authApi.getProfile() on mount with explicit initialization state.
 * 2. Cross-Tab Session Synchronization: Uses BroadcastChannel and Storage Events to sync login/logout/invalidation across open tabs instantly.
 * 3. Graceful Session Expiration UX: Displays a blocking 3-second SessionExpiredModal before redirecting.
 * 4. Single Source of Truth: Centralized login, logout, refresh, and storage operations.
 */

import React, { createContext, useContext, useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { storage, STORAGE_KEYS } from "@/shared/services/storage";
import { registerUnauthorizedHandler, resetUnauthorizedState } from "@/shared/lib/http";
import { authApi } from "@/features/auth/api/auth.api";
import { AuthLoadingScreen } from "@/features/auth/components/AuthLoadingScreen";
import { SessionExpiredModal } from "@/features/auth/components/SessionExpiredModal";
import { ROUTES } from "@/config/routes";

// ── Types ──────────────────────────────────────────────────────────────────────

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role?: string;
  companyId?: string;
  companyName?: string;
}

export type AuthStatus = "initializing" | "authenticated" | "unauthenticated";

export interface AuthContextValue {
  authStatus: AuthStatus;
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserProfile | null;
  login: (userData: UserProfile) => void;
  logout: () => void;
  invalidateSession: (returnUrl?: string) => void;
}

// ── Context ────────────────────────────────────────────────────────────────────

const AuthContext = createContext<AuthContextValue | null>(null);

// ── Storage & Broadcast Keys ──────────────────────────────────────────────────
const AUTH_USER_KEY = "aqari:auth_user";
const CURRENT_COMPANY_KEY = "aqari:current_company";
const SIDEBAR_COLLAPSED_KEY = "aqari:sidebar_collapsed";
const BROADCAST_CHANNEL_NAME = "aqari_auth_channel";

const PUBLIC_UI_ROUTES = [
  "/auth/login",
  "/auth/register",
  "/auth/forgot-password",
  "/auth/otp",
];

function isPublicUiRoute(path: string): boolean {
  const normalized = path.toLowerCase();
  return PUBLIC_UI_ROUTES.some((route) => normalized.startsWith(route));
}

function purgeStorage(): void {
  try {
    localStorage.removeItem(AUTH_USER_KEY);
    localStorage.removeItem(CURRENT_COMPANY_KEY);
    storage.remove(STORAGE_KEYS.accessToken);
    storage.remove(STORAGE_KEYS.refreshToken);
  } catch {
    // Fail-safe
  }
}

// ── Sidebar state helpers ─────────────────────────────────────────────────────
export function getSidebarCollapsed(): boolean {
  try {
    return localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === "true";
  } catch {
    return false;
  }
}

export function setSidebarCollapsed(value: boolean): void {
  try {
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(value));
  } catch {
    // fail silently
  }
}

// ── Provider ──────────────────────────────────────────────────────────────────

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [authStatus, setAuthStatus] = useState<AuthStatus>("initializing");
  const [sessionExpiredModalOpen, setSessionExpiredModalOpen] = useState(false);
  const [pendingReturnUrl, setPendingReturnUrl] = useState<string | undefined>(undefined);

  const queryClient = useQueryClient();

  // Helper for cross-tab broadcast messaging
  const broadcastAuthEvent = useCallback((type: "LOGOUT" | "LOGIN" | "SESSION_INVALIDATED", payload?: any) => {
    if (typeof BroadcastChannel !== "undefined") {
      try {
        const channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
        channel.postMessage({ type, payload });
        channel.close();
      } catch {
        // BroadcastChannel unavailable
      }
    }
  }, []);

  /**
   * Requirement 2: Startup Session Validation
   * Validates access token against backend API on startup before marking user authenticated.
   */
  useEffect(() => {
    let isMounted = true;

    async function initializeSession() {
      const token = storage.get<string>(STORAGE_KEYS.accessToken);
      const rawUser = localStorage.getItem(AUTH_USER_KEY);

      if (!token) {
        purgeStorage();
        if (isMounted) {
          setUser(null);
          setAuthStatus("unauthenticated");
        }
        return;
      }

      try {
        // Validate session with backend /api/v1/auth/me endpoint
        const profile = await authApi.getProfile();
        const verifiedUser: UserProfile = {
          id: profile.id,
          name: profile.fullName,
          email: profile.email || profile.phone || "",
          role: profile.companyRoles?.[0]?.roleName || "user",
          companyId: profile.companyRoles?.[0]?.companyId,
          companyName: profile.companyRoles?.[0]?.companyName,
        };

        if (isMounted) {
          localStorage.setItem(AUTH_USER_KEY, JSON.stringify(verifiedUser));
          setUser(verifiedUser);
          setAuthStatus("authenticated");
        }
      } catch (err: any) {
        // If GET /me fails with 401, attempt a silent token refresh
        if (err?.status === 401) {
          try {
            const refreshRes = await authApi.refreshToken();
            storage.set(STORAGE_KEYS.accessToken, refreshRes.accessToken);
            
            const profile = await authApi.getProfile();
            const verifiedUser: UserProfile = {
              id: profile.id,
              name: profile.fullName,
              email: profile.email || profile.phone || "",
              role: profile.companyRoles?.[0]?.roleName || "user",
            };

            if (isMounted) {
              localStorage.setItem(AUTH_USER_KEY, JSON.stringify(verifiedUser));
              setUser(verifiedUser);
              setAuthStatus("authenticated");
            }
          } catch {
            // Refresh failed — clear invalid session
            purgeStorage();
            if (isMounted) {
              setUser(null);
              setAuthStatus("unauthenticated");
            }
          }
        } else if (rawUser) {
          // Fallback for network error (offline start with existing token)
          try {
            const parsed = JSON.parse(rawUser);
            if (isMounted) {
              setUser(parsed);
              setAuthStatus("authenticated");
            }
          } catch {
            purgeStorage();
            if (isMounted) {
              setUser(null);
              setAuthStatus("unauthenticated");
            }
          }
        } else {
          purgeStorage();
          if (isMounted) {
            setUser(null);
            setAuthStatus("unauthenticated");
          }
        }
      }
    }

    initializeSession();

    return () => {
      isMounted = false;
    };
  }, []);

  /**
   * Requirement 1: Cross-Tab Session Synchronization
   * Listens for BroadcastChannel messages & localStorage changes across open tabs.
   */
  useEffect(() => {
    const handleSyncLogout = () => {
      try {
        queryClient.cancelQueries();
        queryClient.clear();
      } catch {
        // Fail-safe
      }
      setUser(null);
      setAuthStatus("unauthenticated");
      setSessionExpiredModalOpen(false);

      if (!isPublicUiRoute(window.location.pathname)) {
        window.location.replace(ROUTES.auth.login);
      }
    };

    const handleSyncLogin = (userData: UserProfile) => {
      setUser(userData);
      setAuthStatus("authenticated");
      resetUnauthorizedState();
    };

    // 1. Storage Event listener for tab cross-sync
    const handleStorageEvent = (e: StorageEvent) => {
      if (e.key === AUTH_USER_KEY && !e.newValue) {
        handleSyncLogout();
      } else if (e.key === AUTH_USER_KEY && e.newValue) {
        try {
          handleSyncLogin(JSON.parse(e.newValue));
        } catch {
          // Ignore
        }
      }
    };

    window.addEventListener("storage", handleStorageEvent);

    // 2. BroadcastChannel listener for active tab messaging
    let authChannel: BroadcastChannel | null = null;
    if (typeof BroadcastChannel !== "undefined") {
      try {
        authChannel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
        authChannel.onmessage = (event) => {
          if (event.data?.type === "LOGOUT" || event.data?.type === "SESSION_INVALIDATED") {
            handleSyncLogout();
          } else if (event.data?.type === "LOGIN" && event.data?.payload) {
            handleSyncLogin(event.data.payload);
          }
        };
      } catch {
        // BroadcastChannel unsupported
      }
    }

    return () => {
      window.removeEventListener("storage", handleStorageEvent);
      authChannel?.close();
    };
  }, [queryClient]);

  /**
   * Performs the physical redirection to Login page.
   */
  const executeLoginRedirect = useCallback((targetReturnUrl?: string) => {
    setSessionExpiredModalOpen(false);
    const currentPath = window.location.pathname;

    if (isPublicUiRoute(currentPath)) {
      return;
    }

    const targetUrl = targetReturnUrl || (currentPath !== ROUTES.root ? currentPath + window.location.search : "");
    const loginRedirectUrl = targetUrl 
      ? `${ROUTES.auth.login}?returnUrl=${encodeURIComponent(targetUrl)}`
      : ROUTES.auth.login;

    window.location.replace(loginRedirectUrl);
  }, []);

  /**
   * Requirement 3: Session Expiration UX
   * Triggered when a 401 occurs. Opens SessionExpiredModal for 3 seconds before redirecting.
   */
  const invalidateSession = useCallback((explicitReturnUrl?: string) => {
    // Cancel queries and purge storage synchronously
    try {
      queryClient.cancelQueries();
      queryClient.clear();
    } catch {
      // Fail-safe
    }

    purgeStorage();
    setUser(null);
    setAuthStatus("unauthenticated");

    // Broadcast invalidation event to other tabs
    broadcastAuthEvent("SESSION_INVALIDATED");

    const currentPath = window.location.pathname;
    if (isPublicUiRoute(currentPath)) {
      return;
    }

    // Open blocking dialog for graceful UX
    setPendingReturnUrl(explicitReturnUrl || currentPath + window.location.search);
    setSessionExpiredModalOpen(true);
  }, [queryClient, broadcastAuthEvent]);

  /**
   * Register global 401 Unauthorized handler with HttpClient
   */
  useEffect(() => {
    registerUnauthorizedHandler((returnUrl) => {
      invalidateSession(returnUrl);
    });
  }, [invalidateSession]);

  /**
   * Login helper — persists user, broadcasts event, resets unauthorized state.
   */
  const login = useCallback((userData: UserProfile) => {
    try {
      localStorage.setItem(AUTH_USER_KEY, JSON.stringify(userData));
    } catch {
      // Storage unavailable
    }
    resetUnauthorizedState();
    setUser(userData);
    setAuthStatus("authenticated");
    broadcastAuthEvent("LOGIN", userData);
  }, [broadcastAuthEvent]);

  /**
   * Explicit user-initiated logout.
   */
  const logout = useCallback(() => {
    try {
      queryClient.cancelQueries();
      queryClient.clear();
    } catch {
      // Fail-safe
    }

    purgeStorage();
    setUser(null);
    setAuthStatus("unauthenticated");
    broadcastAuthEvent("LOGOUT");

    if (!isPublicUiRoute(window.location.pathname)) {
      window.location.replace(ROUTES.auth.login);
    }
  }, [queryClient, broadcastAuthEvent]);

  // Requirement 2: Render full-screen loading state during startup initialization to avoid flicker
  if (authStatus === "initializing") {
    return <AuthLoadingScreen />;
  }

  return (
    <AuthContext.Provider
      value={{
        authStatus,
        isAuthenticated: authStatus === "authenticated" && !!user,
        isLoading: authStatus === "initializing",
        user,
        login,
        logout,
        invalidateSession,
      }}
    >
      {children}

      {/* Requirement 3: Blocking 3-second Session Expired Dialog */}
      <SessionExpiredModal
        isOpen={sessionExpiredModalOpen}
        onConfirm={() => executeLoginRedirect(pendingReturnUrl)}
      />
    </AuthContext.Provider>
  );
}

// ── Hook ──────────────────────────────────────────────────────────────────────

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
