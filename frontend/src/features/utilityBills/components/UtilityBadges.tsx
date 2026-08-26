import { StatusBadge, type StatusVariant } from "@/shared/components/ui/StatusBadge";
import { useTranslation } from "@/shared/i18n";
import type { UtilityBillPaymentStatus, UtilityProviderAvailability, UtilitySyncStatus } from "../types/utilityBills.types";
import { paymentStatusName, providerAvailabilityName, syncStatusName } from "../utils/utilityDisplay";

const syncVariants: Record<string, StatusVariant> = { Synced: "success", Syncing: "info", NeverSynced: "neutral", ProviderError: "danger", RateLimited: "warning", Timeout: "warning", Suspended: "danger", InvalidAccount: "danger" };
const providerVariants: Record<string, StatusVariant> = { Configured: "success", Disabled: "warning", NotConfigured: "danger" };
const paymentVariants: Record<string, StatusVariant> = { Paid: "success", Unpaid: "warning", Unknown: "neutral" };

function translated(language: string, group: "sync" | "provider" | "payment", name: string) {
  const ar: Record<string, string> = {
    NeverSynced: "لم تتم المزامنة", Syncing: "جارٍ التزامن", Synced: "متزامن", ProviderError: "خطأ المزوّد", RateLimited: "محدود مؤقتاً", Timeout: "انتهت المهلة", Suspended: "معلّق", InvalidAccount: "رقم اشتراك غير صحيح",
    Configured: "مهيأ", Disabled: "معطّل", NotConfigured: "غير مهيأ", Paid: "مدفوعة", Unpaid: "غير مدفوعة", Unknown: "غير معروف",
  };
  const en: Record<string, string> = { NeverSynced: "Never synced", ProviderError: "Provider error", RateLimited: "Rate limited", InvalidAccount: "Invalid account", NotConfigured: "Not configured" };
  return language === "ar" ? ar[name] ?? name : en[name] ?? name.replace(/([a-z])([A-Z])/g, "$1 $2");
}
export function SyncBadge({ value }: { value: UtilitySyncStatus }) { const { language } = useTranslation(); const name = syncStatusName(value); return <StatusBadge label={translated(language, "sync", name)} variant={syncVariants[name] ?? "neutral"} size="sm" />; }
export function ProviderBadge({ value }: { value: UtilityProviderAvailability }) { const { language } = useTranslation(); const name = providerAvailabilityName(value); return <StatusBadge label={translated(language, "provider", name)} variant={providerVariants[name] ?? "neutral"} size="sm" />; }
export function PaymentBadge({ value }: { value: UtilityBillPaymentStatus }) { const { language } = useTranslation(); const name = paymentStatusName(value); return <StatusBadge label={translated(language, "payment", name)} variant={paymentVariants[name] ?? "neutral"} size="sm" />; }
