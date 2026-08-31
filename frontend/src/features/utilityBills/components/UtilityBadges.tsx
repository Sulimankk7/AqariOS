import { StatusBadge, type StatusVariant } from "@/shared/components/ui/StatusBadge";
import { useEntityLabel } from "@/shared/i18n";
import type { UtilityBillPaymentStatus, UtilityProviderAvailability, UtilitySyncStatus } from "../types/utilityBills.types";
import { paymentStatusName, providerAvailabilityName, syncStatusName } from "../utils/utilityDisplay";

const syncVariants: Record<string, StatusVariant> = { Synced: "success", Syncing: "info", NeverSynced: "neutral", ProviderError: "danger", RateLimited: "warning", Timeout: "warning", Suspended: "danger", InvalidAccount: "danger" };
const providerVariants: Record<string, StatusVariant> = { Configured: "success", Disabled: "warning", NotConfigured: "danger" };
const paymentVariants: Record<string, StatusVariant> = { Paid: "success", Unpaid: "warning", Unknown: "neutral" };

export function SyncBadge({ value }: { value: UtilitySyncStatus }) { const label = useEntityLabel(); const name = syncStatusName(value); return <StatusBadge label={label("utilitySyncStatus", name)} variant={syncVariants[name] ?? "neutral"} size="sm" />; }
export function ProviderBadge({ value }: { value: UtilityProviderAvailability }) { const label = useEntityLabel(); const name = providerAvailabilityName(value); return <StatusBadge label={label("utilityProviderAvailability", name)} variant={providerVariants[name] ?? "neutral"} size="sm" />; }
export function PaymentBadge({ value }: { value: UtilityBillPaymentStatus }) { const label = useEntityLabel(); const name = paymentStatusName(value); return <StatusBadge label={label("utilityPaymentStatus", name)} variant={paymentVariants[name] ?? "neutral"} size="sm" />; }
