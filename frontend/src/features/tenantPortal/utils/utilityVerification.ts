import { useSyncExternalStore } from "react";
import type {
  TenantUtilityAccountDto,
  TenantUtilityDashboardSummaryDto,
  TenantUtilityTypeName,
} from "../types/utilityBills.types";
import { tenantSyncStatusName } from "./utilityBills";

export const UTILITY_VERIFICATION_INTERVAL_MS = 3_000;
export const UTILITY_VERIFICATION_TIMEOUT_MS = 120_000;

export type UtilityVerificationPhase = "polling" | "timedOut";

export interface UtilityVerificationWindow {
  accountId: string;
  startedAt: number;
  expiresAt: number;
  baselineStatus?: string;
  baselineLastSuccessfulSyncAt?: string | null;
  utilityType?: TenantUtilityTypeName;
  sawTransientState: boolean;
  phase: UtilityVerificationPhase;
}

export interface UtilityVerificationEvaluation {
  window: UtilityVerificationWindow | null;
  shouldPoll: boolean;
  completedSuccessfully: boolean;
}

const transientStatuses = new Set(["NeverSynced", "Syncing"]);
const windowsByUser = new Map<string, Map<string, UtilityVerificationWindow>>();
const listeners = new Set<() => void>();
let snapshotVersion = 0;

function emitChange() {
  snapshotVersion += 1;
  listeners.forEach((listener) => listener());
}

function userKey(userId?: string) {
  return userId ?? "anonymous";
}

function accountSnapshot(account?: TenantUtilityAccountDto) {
  return {
    status: account ? tenantSyncStatusName(account.syncStatus) : undefined,
    lastSuccessfulSyncAt: account?.lastSuccessfulSyncAt ?? null,
  };
}

export function createUtilityVerificationWindow(
  accountId: string,
  account?: TenantUtilityAccountDto,
  now = Date.now(),
): UtilityVerificationWindow {
  const baseline = accountSnapshot(account);
  return {
    accountId,
    startedAt: now,
    expiresAt: now + UTILITY_VERIFICATION_TIMEOUT_MS,
    baselineStatus: baseline.status,
    baselineLastSuccessfulSyncAt: baseline.lastSuccessfulSyncAt,
    utilityType: account ? (account.utilityType === 1 || account.utilityType === "Water" ? "Water" : "Electricity") : undefined,
    sawTransientState: baseline.status ? transientStatuses.has(baseline.status) : false,
    phase: "polling",
  };
}

export function evaluateUtilityVerification(
  window: UtilityVerificationWindow,
  account: TenantUtilityAccountDto | undefined,
  now = Date.now(),
): UtilityVerificationEvaluation {
  if (window.phase === "timedOut") {
    return { window, shouldPoll: false, completedSuccessfully: false };
  }

  if (now >= window.expiresAt) {
    return {
      window: { ...window, phase: "timedOut" },
      shouldPoll: false,
      completedSuccessfully: false,
    };
  }

  if (!account) {
    return { window, shouldPoll: true, completedSuccessfully: false };
  }

  const status = tenantSyncStatusName(account.syncStatus);
  if (transientStatuses.has(status)) {
    return {
      window: window.sawTransientState ? window : { ...window, sawTransientState: true },
      shouldPoll: true,
      completedSuccessfully: false,
    };
  }

  const successfulSyncChanged = status === "Synced"
    && account.lastSuccessfulSyncAt !== window.baselineLastSuccessfulSyncAt;
  const terminalStateChanged = status !== window.baselineStatus;

  if (window.sawTransientState || successfulSyncChanged || terminalStateChanged) {
    return {
      window: null,
      shouldPoll: false,
      completedSuccessfully: status === "Synced",
    };
  }

  // Manual sync can be accepted while the account still exposes its old terminal
  // state. Keep polling until the worker claims it, a new successful timestamp is
  // observed, another terminal state appears, or the bounded window expires.
  return { window, shouldPoll: true, completedSuccessfully: false };
}

export function startUtilityVerification(
  userId: string | undefined,
  accountId: string,
  account?: TenantUtilityAccountDto,
) {
  const key = userKey(userId);
  const windows = new Map(windowsByUser.get(key) ?? []);
  windows.set(accountId, createUtilityVerificationWindow(accountId, account));
  windowsByUser.set(key, windows);
  emitChange();
}

export function ensureTransientUtilityVerifications(
  userId: string | undefined,
  accounts: TenantUtilityAccountDto[],
) {
  const key = userKey(userId);
  const existing = windowsByUser.get(key) ?? new Map<string, UtilityVerificationWindow>();
  let next = existing;
  let changed = false;

  for (const account of accounts) {
    if (account.unlinkedAt || !transientStatuses.has(tenantSyncStatusName(account.syncStatus))) continue;
    if (existing.has(account.id)) continue;
    if (!changed) next = new Map(existing);
    next.set(account.id, createUtilityVerificationWindow(account.id, account));
    changed = true;
  }

  if (changed) {
    windowsByUser.set(key, next);
    emitChange();
  }
}

export function evaluateUtilityVerificationWindows(
  userId: string | undefined,
  accounts: TenantUtilityAccountDto[],
) {
  const key = userKey(userId);
  const existing = windowsByUser.get(key);
  if (!existing?.size) return { shouldPoll: false, completedSuccessfully: false };

  const accountsById = new Map(accounts.map((account) => [account.id, account]));
  const next = new Map(existing);
  let shouldPoll = false;
  let completedSuccessfully = false;
  let changed = false;

  for (const [accountId, window] of existing) {
    const result = evaluateUtilityVerification(window, accountsById.get(accountId));
    shouldPoll ||= result.shouldPoll;
    completedSuccessfully ||= result.completedSuccessfully;

    if (result.window === null) {
      next.delete(accountId);
      changed = true;
    } else if (result.window !== window) {
      next.set(accountId, result.window);
      changed = true;
    }
  }

  if (changed) {
    windowsByUser.set(key, next);
    emitChange();
  }

  return { shouldPoll, completedSuccessfully };
}

export function evaluateUtilitySummaryVerificationWindows(
  userId: string | undefined,
  summary: TenantUtilityDashboardSummaryDto | undefined,
) {
  const key = userKey(userId);
  const existing = windowsByUser.get(key);
  if (!existing?.size) return false;

  const syntheticAccounts: TenantUtilityAccountDto[] = [];
  for (const [accountId, window] of existing) {
    if (!summary || !window.utilityType) continue;
    const isWater = window.utilityType === "Water";
    const linked = isWater ? summary.waterLinked : summary.electricityLinked;
    const syncStatus = isWater ? summary.waterSyncStatus : summary.electricitySyncStatus;
    if (!linked || syncStatus == null) continue;
    syntheticAccounts.push({
      id: accountId,
      utilityType: window.utilityType,
      accountNumber: isWater ? summary.waterAccountNumber ?? "" : summary.electricityAccountNumber ?? "",
      isActive: true,
      syncStatus,
      lastSuccessfulSyncAt: isWater
        ? summary.waterLastSuccessfulSyncAt
        : summary.electricityLastSuccessfulSyncAt,
      historicalBootstrapCompleted: false,
    });
  }

  return evaluateUtilityVerificationWindows(userId, syntheticAccounts).shouldPoll;
}

export function useUtilityVerificationStates(userId?: string) {
  useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => snapshotVersion,
    () => snapshotVersion,
  );

  return windowsByUser.get(userKey(userId)) ?? new Map<string, UtilityVerificationWindow>();
}
