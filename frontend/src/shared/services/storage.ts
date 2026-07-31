/**
 * Browser storage service.
 * Wraps localStorage and sessionStorage with type-safety, JSON parsing,
 * and graceful fallback when storage is unavailable (SSR / private mode).
 *
 * Architecture Rule:
 * Refresh tokens are managed EXCLUSIVELY by the backend via HttpOnly cookies.
 * The client MUST NOT store or persist refresh tokens in client storage.
 */

function safeGet(store: Storage, key: string): string | null {
  try {
    return store.getItem(key);
  } catch {
    return null;
  }
}

function safeSet(store: Storage, key: string, value: string): void {
  try {
    store.setItem(key, value);
  } catch {
    // Quota exceeded or storage unavailable — fail silently
  }
}

function safeRemove(store: Storage, key: string): void {
  try {
    store.removeItem(key);
  } catch {
    // Fail silently
  }
}

// ── Storage Keys ──────────────────────────────────────────────────────────────

/** All storage keys managed by client application. */
export const STORAGE_KEYS = {
  accessToken: "aqari:access_token",
  preferredLanguage: "aqari:preferred_language",
  rememberMe: "aqari:remember_me",
} as const;

export type StorageKey = (typeof STORAGE_KEYS)[keyof typeof STORAGE_KEYS];

// ── Local Storage helpers ─────────────────────────────────────────────────────

export const storage = {
  /** Read a value from localStorage. */
  get<T>(key: StorageKey): T | null {
    const raw = safeGet(localStorage, key);
    if (raw === null) return null;
    try {
      return JSON.parse(raw) as T;
    } catch {
      return raw as unknown as T;
    }
  },

  /** Write a JSON-serialisable value to localStorage. */
  set<T>(key: StorageKey, value: T): void {
    safeSet(localStorage, key, JSON.stringify(value));
  },

  /** Remove a key from localStorage. */
  remove(key: StorageKey): void {
    safeRemove(localStorage, key);
  },

  /** Clear all AqariOS-namespaced keys from localStorage. */
  clearAll(): void {
    Object.values(STORAGE_KEYS).forEach((k) => safeRemove(localStorage, k));
  },
};

// ── Session Storage helpers ───────────────────────────────────────────────────

export const sessionStorage_ = {
  get<T>(key: string): T | null {
    const raw = safeGet(sessionStorage, key);
    if (raw === null) return null;
    try {
      return JSON.parse(raw) as T;
    } catch {
      return raw as unknown as T;
    }
  },

  set<T>(key: string, value: T): void {
    safeSet(sessionStorage, key, value);
  },

  remove(key: string): void {
    safeRemove(sessionStorage, key);
  },
};
