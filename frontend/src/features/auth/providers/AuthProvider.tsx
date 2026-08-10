/**
 * AuthProvider — Single Source of Truth for Authentication Architecture.
 *
 * Implements:
 * 1. Startup Session Validation: Validates tokens via authApi.getProfile() on mount with explicit initialization state.
 * 2. Cross-Tab Session Synchronization: Uses BroadcastChannel and Storage Events to sync login/logout/invalidation across open tabs instantly.
 * 3. Graceful Session Expiration UX: Displays a blocking 3-second SessionExpiredModal before redirecting.
 * 4. Single Source of Truth: Centralized login, logout, refresh, and storage operations.
 * 5. Strict Role Resolution: Resolves canonical roleCode (COMPANY_ADMIN | TENANT) without default fallbacks.
 */

import React, { createContext, useContext, useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { storage, STORAGE_KEYS } from "@/shared/services/storage";
import { registerUnauthorizedHandler, resetUnauthorizedState } from "@/shared/lib/http";
import { authApi } from "@/features/auth/api/auth.api";
import { AuthLoadingScreen } from "@/features/auth/components/AuthLoadingScreen";
import { SessionExpiredModal } from "@/features/auth/components/SessionExpiredModal";
import { ROUTES } from "@/config/routes";
import type { UserProfileDto } from "@/features/auth/types/auth.types";

// ── Types ──────────────────────────────────────────────────────────────────────

export type AppRole = "COMPANY_ADMIN" | "TENANT";

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  roleCode: AppRole;
  companyId?: string;
}

export type AuthStatus = "initializing" | "authenticated" | "unauthenticated" | "unsupported_role";

export interface AuthContextValue {
  authStatus: AuthStatus;
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserProfile | null;
  login: (accessToken: string, profileDto?: UserProfileDto) => Promise<UserProfile>;
  logout: () => void;
  invalidateSession: (returnUrl?: string) => void;
}

// ── Role Resolution Helper ─────────────────────────────────────────────────────

/**
 * Resolves the canonical roleCode from UserProfileDto.
 * Never defaults to COMPANY_ADMIN or user.
 * Evaluates activeCompanyId first if present, otherwise finds a valid system role.
 */
export function resolveUserRole(profile: UserProfileDto): { roleCode: AppRole | undefined; companyId: string | undefined } {
  if (!profile || !profile.companyRoles || profile.companyRoles.length === 0) {
    return { roleCode: undefined, companyId: undefined };
  }

  if (profile.activeCompanyId) {
    const matchingRole = profile.companyRoles.find((r) => r.companyId === profile.activeCompanyId);
    if (matchingRole && (matchingRole.roleCode === "COMPANY_ADMIN" || matchingRole.roleCode === "TENANT")) {
      return {
        roleCode: matchingRole.roleCode as AppRole,
        companyId: matchingRole.companyId,
      };
    }
  }

  const validRole = profile.companyRoles.find((r) => r.roleCode === "COMPANY_ADMIN" || r.roleCode === "TENANT");
  if (validRole) {
    return {
      roleCode: validRole.roleCode as AppRole,
      companyId: validRole.companyId,
    };
  }

  return { roleCode: undefined, companyId: undefined };
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
   * Startup Session Validation
   * Validates access token against backend API on startup before marking user authenticated.
   */
  useEffect(() => {
    let isMounted = true;

    async function initializeSession() {
      const token = storage.get<string>(STORAGE_KEYS.accessToken);

      if (!token) {
        purgeStorage();
        if (isMounted) {
          setUser(null);
          setAuthStatus("unauthenticated");
        }
        return;
      }

      try {
        const profile = await authApi.getProfile();
        const { roleCode, companyId } = resolveUserRole(profile);

        if (!roleCode) {
          purgeStorage();
          if (isMounted) {
            setUser(null);
            setAuthStatus("unsupported_role");
          }
          return;
        }

        const verifiedUser: UserProfile = {
          id: profile.id,
          name: profile.fullName,
          email: profile.email || profile.phone || "",
          roleCode,
          companyId,
        };

        if (isMounted) {
          localStorage.setItem(AUTH_USER_KEY, JSON.stringify(verifiedUser));
          setUser(verifiedUser);
          setAuthStatus("authenticated");
        }
      } catch (err: any) {
        if (err?.status === 401) {
          try {
            const refreshRes = await authApi.refreshToken();
            storage.set(STORAGE_KEYS.accessToken, refreshRes.accessToken);
            
            const profile = await authApi.getProfile();
            const { roleCode, companyId } = resolveUserRole(profile);

            if (!roleCode) {
              purgeStorage();
              if (isMounted) {
                setUser(null);
                setAuthStatus("unsupported_role");
              }
              return;
            }

            const verifiedUser: UserProfile = {
              id: profile.id,
              name: profile.fullName,
              email: profile.email || profile.phone || "",
              roleCode,
              companyId,
            };

            if (isMounted) {
              localStorage.setItem(AUTH_USER_KEY, JSON.stringify(verifiedUser));
              setUser(verifiedUser);
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
          const rawUser = localStorage.getItem(AUTH_USER_KEY);
          if (rawUser) {
            try {
              const parsed: UserProfile = JSON.parse(rawUser);
              if (parsed && (parsed.roleCode === "COMPANY_ADMIN" || parsed.roleCode === "TENANT")) {
                if (isMounted) {
                  setUser(parsed);
                  setAuthStatus("authenticated");
                }
                return;
              }
            } catch {
              // Ignore
            }
          }
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
   * Cross-Tab Session Synchronization
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

  const invalidateSession = useCallback((explicitReturnUrl?: string) => {
    try {
      queryClient.cancelQueries();
      queryClient.clear();
    } catch {
      // Fail-safe
    }

    purgeStorage();
    setUser(null);
    setAuthStatus("unauthenticated");

    broadcastAuthEvent("SESSION_INVALIDATED");

    const currentPath = window.location.pathname;
    if (isPublicUiRoute(currentPath)) {
      return;
    }

    setPendingReturnUrl(explicitReturnUrl || currentPath + window.location.search);
    setSessionExpiredModalOpen(true);
  }, [queryClient, broadcastAuthEvent]);

  useEffect(() => {
    registerUnauthorizedHandler((returnUrl) => {
      invalidateSession(returnUrl);
    });
  }, [invalidateSession]);

  /**
   * Atomic Login Helper — Purges old state/cache, resolves canonical roleCode, and commits state.
   */
  const login = useCallback(async (accessToken: string, profileDto?: UserProfileDto): Promise<UserProfile> => {
    // 1. Synchronously purge previous session and clear query cache
    try {
      queryClient.cancelQueries();
      queryClient.clear();
    } catch {
      // Fail-safe
    }

    // 2. Persist token
    storage.set(STORAGE_KEYS.accessToken, accessToken);

    // 3. Obtain profile if not supplied or roles missing
    let profile = profileDto;
    if (!profile || !profile.companyRoles || profile.companyRoles.length === 0) {
      profile = await authApi.getProfile();
    }

    // 4. Resolve roleCode canonically
    const { roleCode, companyId } = resolveUserRole(profile);

    if (!roleCode) {
      purgeStorage();
      setUser(null);
      setAuthStatus("unsupported_role");
      throw new Error("UNSUPPORTED_ROLE");
    }

    const verifiedUser: UserProfile = {
      id: profile.id,
      name: profile.fullName,
      email: profile.email || profile.phone || "",
      roleCode,
      companyId,
    };

    // 5. Commit state atomically
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(verifiedUser));
    resetUnauthorizedState();
    setUser(verifiedUser);
    setAuthStatus("authenticated");
    broadcastAuthEvent("LOGIN", verifiedUser);

    return verifiedUser;
  }, [queryClient, broadcastAuthEvent]);

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

  if (authStatus === "initializing") {
    return <AuthLoadingScreen />;
  }

  return (
    <AuthContext.Provider
      value={{
        authStatus,
        isAuthenticated: authStatus === "authenticated" && !!user && !!user.roleCode,
        isLoading: authStatus === "initializing",
        user,
        login,
        logout,
        invalidateSession,
      }}
    >
      {children}

      <SessionExpiredModal
        isOpen={sessionExpiredModalOpen}
        onConfirm={() => executeLoginRedirect(pendingReturnUrl)}
      />
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
