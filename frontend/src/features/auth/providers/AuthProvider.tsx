/**
 * AuthProvider — Temporary frontend-only authentication context.
 *
 * Architecture Rule:
 * ONLY this provider may communicate with localStorage.
 * No page, component, hook, or route guard should access localStorage directly.
 *
 * When the real backend is ready, replace the login() and logout() implementations
 * without changing any consumer code.
 */

import React, { createContext, useContext, useState, useCallback, useEffect } from "react";
import { storage, STORAGE_KEYS } from "@/shared/services/storage";

// ── Types ──────────────────────────────────────────────────────────────────────

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role?: string;
}

export interface AuthContextValue {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserProfile | null;
  login: (userData: UserProfile) => void;
  logout: () => void;
}

// ── Context ────────────────────────────────────────────────────────────────────

const AuthContext = createContext<AuthContextValue | null>(null);

// ── Storage Key ───────────────────────────────────────────────────────────────
// We use a dedicated key outside of STORAGE_KEYS since STORAGE_KEYS is typed
// only to known server-auth keys. The sidebar collapse key is also managed here
// to ensure no other file accesses localStorage directly.
const AUTH_USER_KEY = "aqari:auth_user";
const SIDEBAR_COLLAPSED_KEY = "aqari:sidebar_collapsed";

// ── Sidebar state helpers (exported so AppLayout can use them via AuthProvider) ──
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
  const [isLoading, setIsLoading] = useState(true);

  // Bootstrap: read persisted session from storage on first mount
  useEffect(() => {
    try {
      const raw = localStorage.getItem(AUTH_USER_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as UserProfile;
        setUser(parsed);
      }
    } catch {
      // Corrupted storage — start unauthenticated
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Simulate successful login.
   * Persists user profile to localStorage.
   * Replace this body with a real token-exchange call when the backend is ready.
   */
  const login = useCallback((userData: UserProfile) => {
    try {
      localStorage.setItem(AUTH_USER_KEY, JSON.stringify(userData));
    } catch {
      // Storage unavailable — still set in-memory state
    }
    setUser(userData);
  }, []);

  /**
   * Clears the authentication session.
   * Consumers (e.g. Topbar logout button) should also clear the TanStack Query
   * cache via queryClient.clear() before calling logout().
   */
  const logout = useCallback(() => {
    try {
      localStorage.removeItem(AUTH_USER_KEY);
      // Also clear the access token if ever set by real auth
      storage.remove(STORAGE_KEYS.accessToken);
    } catch {
      // Fail silently
    }
    setUser(null);
  }, []);

  if (isLoading) {
    // Render nothing until we know the auth state — prevents flash of wrong page
    return null;
  }

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated: !!user,
        isLoading,
        user,
        login,
        logout,
      }}
    >
      {children}
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
