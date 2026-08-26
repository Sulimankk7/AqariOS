export type UtilityType = "Electricity" | "Water" | 0 | 1;
export type UtilitySyncStatus = "NeverSynced" | "Syncing" | "Synced" | "ProviderError" | "RateLimited" | "Timeout" | "Suspended" | "InvalidAccount" | number;
export type UtilityBillPaymentStatus = "Unpaid" | "Paid" | "Unknown" | number;
export type UtilityProviderAvailability = "Configured" | "Disabled" | "NotConfigured" | number;

export interface UtilityBillDto {
  id: string;
  billDate: string;
  dueDate?: string | null;
  amount: number;
  currency: string;
  isPaid: boolean;
  paymentStatus: UtilityBillPaymentStatus;
  discoveredAt: string;
  utilityAccountId?: string | null;
  sourceAccountNumber?: string | null;
  isCurrentAccount?: boolean | null;
  sourceUtilityType?: UtilityType | null;
  sourceAccountUnlinkedAt?: string | null;
}

export interface UtilityAccountDto {
  id: string;
  utilityType: UtilityType;
  accountNumber: string;
  meterNumber?: string | null;
  isActive: boolean;
  lastKnownBillDate?: string | null;
  lastSuccessfulSyncAt?: string | null;
  syncStatus: UtilitySyncStatus;
  averageBillingIntervalDays?: number | null;
  estimatedNextBillDate?: string | null;
  historicalBootstrapCompleted: boolean;
  latestBill?: UtilityBillDto | null;
  unlinkedAt?: string | null;
  linkedAt?: string | null;
}

export interface ManagementUtilityAccountDto extends UtilityAccountDto {
  providerAvailability: UtilityProviderAvailability;
  lastAttemptedSyncAt?: string | null;
  consecutiveFailureCount: number;
  leaseContractId: string;
  leaseContractNumber: string;
  tenantId: string;
  tenantName: string;
  apartmentId: string;
  unitNumber: string;
  buildingId: string;
  buildingName: string;
  linkedAt: string;
}

export interface KeysetPage<T> {
  items: T[];
  nextCursor?: string | null;
  hasMore: boolean;
}

export interface UtilityAccountFilters {
  utilityType?: "Electricity" | "Water";
  syncStatus?: Exclude<UtilitySyncStatus, number>;
  isActive?: boolean;
  leaseContractId?: string;
  includeUnlinked?: boolean;
}

export interface LinkUtilityAccountRequest {
  leaseContractId: string;
  utilityType: 0 | 1;
  accountNumber: string;
  meterNumber?: string | null;
}

export interface ReplaceUtilityAccountRequest {
  accountNumber: string;
  meterNumber?: string | null;
}
