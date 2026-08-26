import test from "node:test";
import assert from "node:assert/strict";
import { ApiError } from "@/shared/lib/http";
import {
  isValidTenantUtilityAccountNumber,
  groupTenantUtilityBills,
  tenantSyncActionName,
  tenantSyncStatusName,
  tenantUtilityAccountNumberMaxLength,
  utilityBillsErrorMessage,
} from "./utilityBills";

const t = (key: string) => key;

test("tenant account validation matches the Electricity and Water backend contract", () => {
  assert.equal(isValidTenantUtilityAccountNumber("Electricity", "0020013902"), true);
  assert.equal(isValidTenantUtilityAccountNumber("Electricity", "002001390"), false);
  assert.equal(isValidTenantUtilityAccountNumber("Electricity", "002001390A"), false);
  assert.equal(isValidTenantUtilityAccountNumber("Water", "1"), true);
  assert.equal(isValidTenantUtilityAccountNumber("Water", "12345678901234567890"), true);
  assert.equal(isValidTenantUtilityAccountNumber("Water", "123456789012345678901"), false);
  assert.equal(isValidTenantUtilityAccountNumber("Water", "12-A"), false);
  assert.equal(tenantUtilityAccountNumberMaxLength("Electricity"), 10);
  assert.equal(tenantUtilityAccountNumberMaxLength("Water"), 20);
});

test("numeric and named sync states resolve consistently", () => {
  assert.equal(tenantSyncStatusName(0), "NeverSynced");
  assert.equal(tenantSyncStatusName(7), "InvalidAccount");
  assert.equal(tenantSyncStatusName("Suspended"), "Suspended");
});

test("sync actions reuse one endpoint with state-appropriate labels", () => {
  assert.equal(tenantSyncActionName("Synced"), "sync");
  assert.equal(tenantSyncActionName("ProviderError"), "retry");
  assert.equal(tenantSyncActionName("InvalidAccount"), "retry");
  assert.equal(tenantSyncActionName("Suspended"), "reactivate");
});

test("known backend errors map to safe localized keys", () => {
  const contextError = new ApiError(422, "raw backend detail", {
    code: "UTILITY_ACCOUNT_TENANT_CONTEXT_INVALID",
  });
  const concurrencyError = new ApiError(409, "raw backend detail", {
    code: "CONCURRENCY_CONFLICT",
  });

  assert.equal(
    utilityBillsErrorMessage(contextError, t),
    "tenant.utilityBills.errors.tenantContextInvalid",
  );
  assert.equal(
    utilityBillsErrorMessage(concurrencyError, t),
    "tenant.utilityBills.errors.concurrency",
  );
  assert.notEqual(utilityBillsErrorMessage(contextError, t), contextError.detail);
});

test("history preserves current and previous UtilityAccount identities", () => {
  const groups = groupTenantUtilityBills(
    [{
      id: "current",
      utilityType: "Electricity",
      accountNumber: "0020013903",
      isActive: true,
      syncStatus: "Synced",
      historicalBootstrapCompleted: true,
    }],
    [
      {
        id: "bill-current",
        utilityAccountId: "current",
        billDate: "2026-08-01",
        amount: 20,
        currency: "JOD",
        isPaid: false,
        paymentStatus: "Unpaid",
        discoveredAt: "2026-08-02T00:00:00Z",
      },
      {
        id: "bill-previous",
        utilityAccountId: "previous",
        sourceAccountNumber: "0020013902",
        sourceUtilityType: "Electricity",
        sourceAccountUnlinkedAt: "2026-07-15T00:00:00Z",
        isCurrentAccount: false,
        billDate: "2026-07-01",
        amount: 18,
        currency: "JOD",
        isPaid: true,
        paymentStatus: "Paid",
        discoveredAt: "2026-07-02T00:00:00Z",
      },
    ],
  );

  assert.equal(groups.length, 2);
  assert.equal(groups[0].accountId, "current");
  assert.equal(groups[0].isCurrent, true);
  assert.equal(groups[1].accountId, "previous");
  assert.equal(groups[1].accountNumber, "0020013902");
  assert.equal(groups[1].utilityType, "Electricity");
  assert.equal(groups[1].isCurrent, false);
});
