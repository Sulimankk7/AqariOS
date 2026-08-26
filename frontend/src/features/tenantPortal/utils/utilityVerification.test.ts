import test from "node:test";
import assert from "node:assert/strict";
import type { TenantUtilityAccountDto } from "../types/utilityBills.types";
import {
  createUtilityVerificationWindow,
  evaluateUtilityVerification,
  UTILITY_VERIFICATION_INTERVAL_MS,
  UTILITY_VERIFICATION_TIMEOUT_MS,
} from "./utilityVerification";

function account(overrides: Partial<TenantUtilityAccountDto> = {}): TenantUtilityAccountDto {
  return {
    id: "account-1",
    utilityType: "Electricity",
    accountNumber: "0020013902",
    isActive: true,
    syncStatus: "Synced",
    lastSuccessfulSyncAt: "2026-08-25T08:00:00Z",
    historicalBootstrapCompleted: true,
    ...overrides,
  };
}

test("polling uses the required three-second interval and two-minute bound", () => {
  assert.equal(UTILITY_VERIFICATION_INTERVAL_MS, 3_000);
  assert.equal(UTILITY_VERIFICATION_TIMEOUT_MS, 120_000);
});

test("manual sync keeps polling while the old terminal status is still visible", () => {
  const start = 1_000;
  const current = account();
  const window = createUtilityVerificationWindow(current.id, current, start);
  const result = evaluateUtilityVerification(window, current, start + 3_000);

  assert.equal(result.shouldPoll, true);
  assert.equal(result.window?.phase, "polling");
});

test("polling stops after transient state reaches a terminal success", () => {
  const start = 1_000;
  const initial = account({ syncStatus: "NeverSynced", lastSuccessfulSyncAt: null });
  const window = createUtilityVerificationWindow(initial.id, initial, start);
  const result = evaluateUtilityVerification(
    window,
    account({ lastSuccessfulSyncAt: "2026-08-25T08:01:00Z" }),
    start + 6_000,
  );

  assert.equal(result.shouldPoll, false);
  assert.equal(result.completedSuccessfully, true);
  assert.equal(result.window, null);
});

test("suspended retry polls until a worker transition instead of stopping immediately", () => {
  const start = 1_000;
  const suspended = account({ syncStatus: "Suspended", isActive: false });
  const window = createUtilityVerificationWindow(suspended.id, suspended, start);
  const waiting = evaluateUtilityVerification(window, suspended, start + 3_000);
  assert.equal(waiting.shouldPoll, true);

  const syncing = evaluateUtilityVerification(waiting.window!, account({ syncStatus: "Syncing" }), start + 6_000);
  assert.equal(syncing.shouldPoll, true);
  assert.equal(syncing.window?.sawTransientState, true);

  const failed = evaluateUtilityVerification(syncing.window!, account({ syncStatus: "ProviderError" }), start + 9_000);
  assert.equal(failed.shouldPoll, false);
  assert.equal(failed.completedSuccessfully, false);
});

test("polling stops at the bounded timeout", () => {
  const start = 1_000;
  const current = account();
  const window = createUtilityVerificationWindow(current.id, current, start);
  const result = evaluateUtilityVerification(window, current, start + UTILITY_VERIFICATION_TIMEOUT_MS);

  assert.equal(result.shouldPoll, false);
  assert.equal(result.window?.phase, "timedOut");
});

