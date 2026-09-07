import { ApiError } from "@/shared/lib/http";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import type {
  TenantUtilityAccountDto,
  TenantUtilityBillDto,
  TenantUtilityBillPaymentStatus,
  TenantUtilitySyncStatus,
  TenantUtilityType,
  TenantUtilityTypeName,
} from "../types/utilityBills.types";

type Translate = (path: string, params?: Record<string, unknown>) => string;

const utilityTypeNames: TenantUtilityTypeName[] = ["Electricity", "Water"];
const syncStatusNames = ["NeverSynced", "Syncing", "Synced", "ProviderError", "RateLimited", "Timeout", "Suspended", "InvalidAccount"];
const paymentStatusNames = ["Unpaid", "Paid", "Unknown"];

function enumName(value: string | number, names: string[]) {
  return typeof value === "number" ? names[value] ?? "Unknown" : value;
}

export function tenantUtilityTypeName(value: TenantUtilityType): TenantUtilityTypeName {
  return enumName(value, utilityTypeNames) === "Water" ? "Water" : "Electricity";
}

export function tenantSyncStatusName(value: TenantUtilitySyncStatus) {
  return enumName(value, syncStatusNames);
}

export function tenantPaymentStatusName(value: TenantUtilityBillPaymentStatus) {
  return enumName(value, paymentStatusNames);
}

export function isValidTenantUtilityAccountNumber(
  utilityType: TenantUtilityTypeName,
  accountNumber: string,
) {
  const value = accountNumber.trim();
  return utilityType === "Electricity"
    ? /^\d{10}$/.test(value)
    : /^\d{1,20}$/.test(value);
}

export function tenantUtilityAccountNumberMaxLength(utilityType: TenantUtilityTypeName) {
  return utilityType === "Electricity" ? 10 : 20;
}

export function tenantSyncActionName(value: TenantUtilitySyncStatus) {
  const status = tenantSyncStatusName(value);
  if (status === "Suspended") return "reactivate";
  if (["InvalidAccount", "ProviderError", "Timeout", "RateLimited"].includes(status)) return "retry";
  return "sync";
}

export interface TenantUtilityBillGroup {
  accountId: string;
  account?: TenantUtilityAccountDto;
  bills: TenantUtilityBillDto[];
  isCurrent: boolean;
  accountNumber: string;
  utilityType: TenantUtilityTypeName | null;
  unlinkedAt?: string | null;
}

export function groupTenantUtilityBills(
  accounts: TenantUtilityAccountDto[],
  bills: TenantUtilityBillDto[],
): TenantUtilityBillGroup[] {
  const grouped = new Map<string, TenantUtilityBillDto[]>();
  for (const bill of bills) {
    const key = bill.utilityAccountId ?? `unknown:${bill.id}`;
    grouped.set(key, [...(grouped.get(key) ?? []), bill]);
  }

  return [...grouped.entries()].map(([accountId, accountBills]) => {
    const account = accounts.find((item) => item.id === accountId);
    const source = accountBills[0];
    return {
      accountId,
      account,
      bills: accountBills,
      isCurrent: account ? !account.unlinkedAt : Boolean(source?.isCurrentAccount),
      accountNumber: account?.accountNumber ?? source?.sourceAccountNumber ?? "—",
      utilityType: account
        ? tenantUtilityTypeName(account.utilityType)
        : source?.sourceUtilityType != null
          ? tenantUtilityTypeName(source.sourceUtilityType)
          : null,
      unlinkedAt: account?.unlinkedAt ?? source?.sourceAccountUnlinkedAt,
    };
  });
}

export function utilityBillsErrorMessage(error: unknown, t: Translate): string {
  if (typeof navigator !== "undefined" && !navigator.onLine) {
    return t("tenant.utilityBills.errors.network");
  }

  if (!(error instanceof ApiError)) {
    return t("tenant.utilityBills.errors.generic");
  }

  const payload = error.rawPayload as { code?: unknown } | undefined;
  const code = typeof payload?.code === "string" ? payload.code : undefined;
  const codeKeys: Record<string, string> = {
    UTILITY_ACCOUNT_TENANT_CONTEXT_INVALID: "tenantContextInvalid",
    UTILITY_ACCOUNT_NO_ELIGIBLE_LEASE: "noEligibleLease",
    UTILITY_ACCOUNT_ACTIVE_LEASE_AMBIGUOUS: "ambiguousLease",
    UTILITY_ACCOUNT_ALREADY_LINKED: "alreadyLinked",
    UTILITY_ACCOUNT_CONFLICT: "conflict",
    UTILITY_ACCOUNT_NUMBER_INVALID: "validation",
    CONCURRENCY_CONFLICT: "concurrency",
    INTERNAL_ERROR: "server",
  };

  if (code && codeKeys[code]) {
    return t(`tenant.utilityBills.errors.${codeKeys[code]}`);
  }

  const message = error.message.toLowerCase();
  if (message.includes("failed to fetch") || message.includes("network") || message.includes("timeout")) {
    return t("tenant.utilityBills.errors.network");
  }

  const statusKeys: Record<number, string> = {
    400: "validation",
    401: "unauthorized",
    403: "forbidden",
    404: "notFound",
    409: "conflict",
    422: "validation",
    500: "server",
    502: "server",
    503: "server",
    504: "server",
  };

  const fallback = t(`tenant.utilityBills.errors.${statusKeys[error.status] ?? "generic"}`);
  return extractUserFriendlyError(error, fallback);
}
