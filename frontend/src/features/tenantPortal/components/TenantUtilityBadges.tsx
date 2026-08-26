import { StatusBadge, type StatusVariant } from "@/shared/components/ui/StatusBadge";
import { useTranslation } from "@/shared/i18n";
import type { TenantUtilityBillPaymentStatus, TenantUtilitySyncStatus } from "../types/utilityBills.types";
import { tenantPaymentStatusName, tenantSyncStatusName } from "../utils/utilityBills";

const syncVariants: Record<string, StatusVariant> = {
  Synced: "success",
  Syncing: "info",
  NeverSynced: "neutral",
  ProviderError: "danger",
  RateLimited: "warning",
  Timeout: "warning",
  Suspended: "danger",
  InvalidAccount: "danger",
};

const paymentVariants: Record<string, StatusVariant> = {
  Paid: "success",
  Unpaid: "warning",
  Unknown: "neutral",
};

export function TenantSyncBadge({ value }: { value: TenantUtilitySyncStatus }) {
  const { t } = useTranslation();
  const name = tenantSyncStatusName(value);
  return (
    <StatusBadge
      label={t(`tenant.utilityBills.syncStatus.${name}`)}
      variant={syncVariants[name] ?? "neutral"}
      size="sm"
    />
  );
}

export function TenantPaymentBadge({ value }: { value: TenantUtilityBillPaymentStatus }) {
  const { t } = useTranslation();
  const name = tenantPaymentStatusName(value);
  return (
    <StatusBadge
      label={t(`tenant.utilityBills.paymentStatus.${name}`)}
      variant={paymentVariants[name] ?? "neutral"}
      size="sm"
    />
  );
}
