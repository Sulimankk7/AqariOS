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
  /**
   * Read a value. Authentication keys prefer the current tab's sessionStorage
   * and then fall back to persistent localStorage.
   */
  get<T>(key: StorageKey): T | null {
    const isAuthKey = key === STORAGE_KEYS.accessToken || key === STORAGE_KEYS.rememberMe;
    const raw = isAuthKey
      ? safeGet(sessionStorage, key) ?? safeGet(localStorage, key)
      : safeGet(localStorage, key);
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
    if (key === STORAGE_KEYS.accessToken || key === STORAGE_KEYS.rememberMe) {
      safeRemove(sessionStorage, key);
    }
  },

  /** Store an access token according to the server-authoritative session mode. */
  setAccessToken(accessToken: string, isPersistentSession: boolean): void {
    safeRemove(localStorage, STORAGE_KEYS.accessToken);
    safeRemove(sessionStorage, STORAGE_KEYS.accessToken);
    safeRemove(localStorage, STORAGE_KEYS.rememberMe);
    safeRemove(sessionStorage, STORAGE_KEYS.rememberMe);

    const target = isPersistentSession ? localStorage : sessionStorage;
    safeSet(target, STORAGE_KEYS.accessToken, JSON.stringify(accessToken));
    safeSet(target, STORAGE_KEYS.rememberMe, JSON.stringify(isPersistentSession));
  },

  isPersistentSession(): boolean {
    return safeGet(localStorage, STORAGE_KEYS.accessToken) !== null;
  },

  /** Clear all AqariOS-namespaced keys from localStorage. */
  clearAll(): void {
    Object.values(STORAGE_KEYS).forEach((k) => {
      safeRemove(localStorage, k);
      safeRemove(sessionStorage, k);
    });
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
    safeSet(sessionStorage, key, JSON.stringify(value));
  },

  remove(key: string): void {
    safeRemove(sessionStorage, key);
  },
};
