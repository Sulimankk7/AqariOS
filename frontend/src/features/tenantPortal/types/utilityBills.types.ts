export type TenantUtilityTypeName = "Electricity" | "Water";
export type UtilityTypeValue = 0 | 1;
export type TenantUtilityType = TenantUtilityTypeName | UtilityTypeValue;

export type TenantUtilitySyncStatus =
  | "NeverSynced"
  | "Syncing"
  | "Synced"
  | "ProviderError"
  | "RateLimited"
  | "Timeout"
  | "Suspended"
  | "InvalidAccount"
  | number;

export type TenantUtilityBillPaymentStatus = "Unpaid" | "Paid" | "Unknown" | number;

export interface TenantUtilityBillDto {
  id: string;
  billDate: string;
  dueDate?: string | null;
  amount: number;
  currency: string;
  isPaid: boolean;
  paymentStatus: TenantUtilityBillPaymentStatus;
  discoveredAt: string;
  utilityAccountId?: string | null;
  sourceAccountNumber?: string | null;
  isCurrentAccount?: boolean | null;
  sourceUtilityType?: TenantUtilityType | null;
  sourceAccountUnlinkedAt?: string | null;
}

export interface TenantUtilityAccountDto {
  id: string;
  utilityType: TenantUtilityType;
  accountNumber: string;
  meterNumber?: string | null;
  isActive: boolean;
  lastKnownBillDate?: string | null;
  lastSuccessfulSyncAt?: string | null;
  syncStatus: TenantUtilitySyncStatus;
  averageBillingIntervalDays?: number | null;
  estimatedNextBillDate?: string | null;
  historicalBootstrapCompleted: boolean;
  totalOutstandingBalance?: number | null;
  latestBill?: TenantUtilityBillDto | null;
  unlinkedAt?: string | null;
  linkedAt?: string | null;
}

export interface TenantLinkUtilityAccountRequest {
  utilityType: UtilityTypeValue;
  accountNumber: string;
  meterNumber?: string | null;
}

export interface TenantReplaceUtilityAccountRequest {
  accountNumber: string;
  meterNumber?: string | null;
}

export interface KeysetPage<T> {
  items: T[];
  nextCursor?: string | null;
  hasMore: boolean;
}

export interface TenantUtilityDashboardSummaryDto {
  electricityLinked: boolean;
  latestElectricityBill?: TenantUtilityBillDto | null;
  waterLinked: boolean;
  latestWaterBill?: TenantUtilityBillDto | null;
  electricityAccountNumber?: string | null;
  electricitySyncStatus?: TenantUtilitySyncStatus | null;
  electricityLastSuccessfulSyncAt?: string | null;
  waterAccountNumber?: string | null;
  waterSyncStatus?: TenantUtilitySyncStatus | null;
  waterLastSuccessfulSyncAt?: string | null;
}
