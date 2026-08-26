import type { UtilityBillPaymentStatus, UtilityProviderAvailability, UtilitySyncStatus, UtilityType } from "../types/utilityBills.types";

const enumValue = (value: string | number, names: string[]) => typeof value === "number" ? names[value] ?? "Unknown" : String(value);
export const utilityTypeName = (v: UtilityType) => enumValue(v, ["Electricity", "Water"]);
export const syncStatusName = (v: UtilitySyncStatus) => enumValue(v, ["NeverSynced", "Syncing", "Synced", "ProviderError", "RateLimited", "Timeout", "Suspended", "InvalidAccount"]);
export const paymentStatusName = (v: UtilityBillPaymentStatus) => enumValue(v, ["Unpaid", "Paid", "Unknown"]);
export const providerAvailabilityName = (v: UtilityProviderAvailability) => enumValue(v, ["Configured", "Disabled", "NotConfigured"]);
