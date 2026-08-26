import test from "node:test";
import assert from "node:assert/strict";
import { http } from "@/shared/lib/http";
import { tenantUtilityBillsApi } from "./utilityBills.api";

test("tenant utility API uses only the audited tenant routes", async () => {
  const calls: Array<{ method: string; url: string; body?: unknown }> = [];
  const originalGet = http.get;
  const originalPost = http.post;
  const originalDelete = http.delete;

  try {
    http.get = (async (url: string) => {
      calls.push({ method: "GET", url });
      return [];
    }) as typeof http.get;
    http.post = (async (url: string, body?: unknown) => {
      calls.push({ method: "POST", url, body });
      return {};
    }) as typeof http.post;
    http.delete = (async (url: string) => {
      calls.push({ method: "DELETE", url });
      return undefined;
    }) as typeof http.delete;

    await tenantUtilityBillsApi.getAccounts();
    await tenantUtilityBillsApi.linkAccount({ utilityType: 0, accountNumber: "0020013902", meterNumber: null });
    await tenantUtilityBillsApi.linkAccount({ utilityType: 1, accountNumber: "12345", meterNumber: "W-1" });
    await tenantUtilityBillsApi.replaceAccount("account-1", { accountNumber: "0020013903", meterNumber: "E-2" });
    await tenantUtilityBillsApi.unlinkAccount("account-1");
    await tenantUtilityBillsApi.requestSync("account-1");
    await tenantUtilityBillsApi.getBills("Water", 50, "next-cursor");
    await tenantUtilityBillsApi.getDashboardSummary();

    assert.deepEqual(calls, [
      { method: "GET", url: "/api/v1/utility-bills/my/accounts" },
      { method: "POST", url: "/api/v1/utility-bills/my/accounts", body: { utilityType: 0, accountNumber: "0020013902", meterNumber: null } },
      { method: "POST", url: "/api/v1/utility-bills/my/accounts", body: { utilityType: 1, accountNumber: "12345", meterNumber: "W-1" } },
      { method: "POST", url: "/api/v1/utility-bills/my/accounts/account-1/replace", body: { accountNumber: "0020013903", meterNumber: "E-2" } },
      { method: "DELETE", url: "/api/v1/utility-bills/my/accounts/account-1" },
      { method: "POST", url: "/api/v1/utility-bills/my/accounts/account-1/sync", body: undefined },
      { method: "GET", url: "/api/v1/utility-bills/my/bills?utilityType=Water&pageSize=50&cursor=next-cursor" },
      { method: "GET", url: "/api/v1/utility-bills/my/dashboard-summary" },
    ]);
  } finally {
    http.get = originalGet;
    http.post = originalPost;
    http.delete = originalDelete;
  }
});

