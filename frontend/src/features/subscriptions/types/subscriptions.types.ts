export type BillingCycle = "Monthly" | "Yearly";
export type SubscriptionStatus = "Trialing" | "Active" | "PastDue" | "Suspended" | "Cancelled" | "Expired";
export type PlanChangeRequestStatus = "Pending" | "Approved" | "Rejected" | "Cancelled";

export interface SubscriptionPlanDto {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  descriptionEn?: string | null;
  descriptionAr?: string | null;
  monthlyPrice: number;
  yearlyPrice: number;
  currency: string;
  maxBuildings?: number | null;
  maxUsers?: number | null;
  maxStorageMb?: number | null;
  featureFlags: string;
  supportsTrial: boolean;
  trialDurationDays?: number | null;
  isActive: boolean;
  sortOrder: number;
}

export interface PlanPageDto { items: SubscriptionPlanDto[]; page: number; pageSize: number; totalCount: number; }

export interface UserSubscriptionDto {
  id: string; companyId: string; planId: string; planCode: string; planNameEn: string; planNameAr: string;
  status: string; startDate: string; endDate: string; trialEndDate?: string | null;
  priceAtSubscription: number; currencyAtSubscription: string; billingCycle: string; autoRenew: boolean;
  suspendedAt?: string | null; suspensionReason?: string | null; cancelledAt?: string | null;
  cancellationReason?: string | null; expiredAt?: string | null; externalBillingRef?: string | null;
  createdAt: string; updatedAt: string;
}

export interface PlanChangeRequestDto {
  id: string; companyId: string; companyName?: string | null; subscriptionId: string;
  currentPlanId: string; currentPlanNameEn: string; currentPlanNameAr: string;
  requestedPlanId: string; requestedPlanNameEn: string; requestedPlanNameAr: string;
  currentBillingCycle: BillingCycle; requestedBillingCycle: BillingCycle;
  requestedBy: string; requesterName?: string | null; requestedAt: string; status: PlanChangeRequestStatus;
  reviewerId?: string | null; reviewerName?: string | null; reviewedAt?: string | null;
  decisionNote?: string | null; rejectionReason?: string | null;
}

export interface PlanChangeRequestPageDto { items: PlanChangeRequestDto[]; page: number; pageSize: number; totalCount: number; }

export interface SubscriptionAdministrationDto {
  id: string; companyId: string; companyName: string; planId: string; planCode: string;
  planNameEn: string; planNameAr: string; status: SubscriptionStatus; billingCycle: BillingCycle;
  startDate: string; endDate: string; trialEndDate?: string | null; priceAtSubscription: number;
  currencyAtSubscription: string; createdAt: string; updatedAt: string;
}

export interface SubscriptionPageDto { items: SubscriptionAdministrationDto[]; page: number; pageSize: number; totalCount: number; }

export interface PlatformCompanyListItemDto { id: string; displayName: string; }

export interface CreatePlanChangeRequest { requestedPlanId: string; requestedBillingCycle: BillingCycle; }
export interface CreatePlanRequest {
  code: string; nameEn: string; nameAr: string; descriptionEn?: string; descriptionAr?: string;
  monthlyPrice: number; yearlyPrice: number; currency: string; maxBuildings?: number | null;
  maxUsers?: number | null; maxStorageMb?: number | null; featureFlags: string; supportsTrial: boolean;
  trialDurationDays?: number | null; sortOrder: number;
}
export interface CreateCompanySubscriptionRequest {
  companyId: string; planId: string; billingCycle: BillingCycle; startDate: string; endDate: string; trialEndDate?: string | null;
}
export interface RejectPlanChangeRequestRequest { rejectionReason: string; decisionNote?: string; }
export interface ApprovePlanChangeRequestRequest { decisionNote?: string; }
