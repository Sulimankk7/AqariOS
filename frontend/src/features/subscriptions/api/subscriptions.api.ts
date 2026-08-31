import { http } from "@/shared/lib/http";
import type {
  ApprovePlanChangeRequestRequest, BillingCycle, CreateCompanySubscriptionRequest, CreatePlanChangeRequest, CreatePlanRequest,
  PlanChangeRequestDto, PlanChangeRequestPageDto, PlanChangeRequestStatus, PlanPageDto, RejectPlanChangeRequestRequest,
  SubscriptionAdministrationDto, SubscriptionPageDto, SubscriptionPlanDto, SubscriptionStatus, UserSubscriptionDto,
  PlatformCompanyListItemDto,
  PaygUsageSummaryDto,
} from "../types/subscriptions.types";

const companyBase = "/api/v1/subscriptions";
const platformBase = "/api/v1/platform";
const billingCycleValue: Record<BillingCycle, 0 | 1> = { Monthly: 0, Yearly: 1 };
const pricingModelValue = { Fixed: 0, PayAsYouGo: 1 } as const;
type SubscriptionAdministrationWireDto = Omit<SubscriptionAdministrationDto, "billingCycle"> & { billingCycle: BillingCycle | number };
type SubscriptionPageWireDto = Omit<SubscriptionPageDto, "items"> & { items: SubscriptionAdministrationWireDto[] };
type PlanChangeRequestWireDto = Omit<PlanChangeRequestDto, "currentBillingCycle" | "requestedBillingCycle" | "status"> & { currentBillingCycle: BillingCycle | number; requestedBillingCycle: BillingCycle | number; status: PlanChangeRequestStatus | number };
type PlanChangeRequestPageWireDto = Omit<PlanChangeRequestPageDto, "items"> & { items: PlanChangeRequestWireDto[] };
type SubscriptionPlanWireDto = Omit<SubscriptionPlanDto, "pricingModel"> & { pricingModel: SubscriptionPlanDto["pricingModel"] | number };
type PlanPageWireDto = Omit<PlanPageDto, "items"> & { items: SubscriptionPlanWireDto[] };
type PaygUsageWireDto = Omit<PaygUsageSummaryDto, "pricingModel" | "subscriptionStatus" | "billingCycle"> & { pricingModel: PaygUsageSummaryDto["pricingModel"] | number; subscriptionStatus: SubscriptionStatus | number; billingCycle: BillingCycle | number };
const normalizeBillingCycle = (value: BillingCycle | number): BillingCycle => value === 1 || value === "Yearly" ? "Yearly" : "Monthly";
const normalizeSubscription = (item: SubscriptionAdministrationWireDto): SubscriptionAdministrationDto => ({ ...item, billingCycle: normalizeBillingCycle(item.billingCycle) });
const requestStatuses: PlanChangeRequestStatus[] = ["Pending", "Approved", "Rejected", "Cancelled"];
const subscriptionStatuses: SubscriptionStatus[] = ["Trialing", "Active", "PastDue", "Suspended", "Cancelled", "Expired"];
const normalizeRequest = (item: PlanChangeRequestWireDto): PlanChangeRequestDto => ({ ...item, currentBillingCycle: normalizeBillingCycle(item.currentBillingCycle), requestedBillingCycle: normalizeBillingCycle(item.requestedBillingCycle), status: typeof item.status === "number" ? requestStatuses[item.status] : item.status });
const normalizeCurrentSubscription = (item: UserSubscriptionDto): UserSubscriptionDto => ({ ...item, billingCycle: normalizeBillingCycle(item.billingCycle as BillingCycle | number), status: typeof item.status === "number" ? subscriptionStatuses[item.status] : `${item.status.charAt(0).toUpperCase()}${item.status.slice(1)}` });
const normalizeUsage = (item: PaygUsageWireDto): PaygUsageSummaryDto => ({ ...item, pricingModel: item.pricingModel === 1 || item.pricingModel === "PayAsYouGo" ? "PayAsYouGo" : "Fixed", billingCycle: normalizeBillingCycle(item.billingCycle), subscriptionStatus: typeof item.subscriptionStatus === "number" ? subscriptionStatuses[item.subscriptionStatus] : item.subscriptionStatus });
const normalizePlan = (item: SubscriptionPlanWireDto): SubscriptionPlanDto => ({ ...item, pricingModel: item.pricingModel === 1 || item.pricingModel === "PayAsYouGo" ? "PayAsYouGo" : "Fixed" });
const normalizePlanPage = (page: PlanPageWireDto): PlanPageDto => ({ ...page, items: page.items.map(normalizePlan) });
const query = (values: Record<string, string | number | boolean | undefined | null>) => {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => { if (value !== undefined && value !== null && value !== "") params.set(key, String(value)); });
  return params.toString() ? `?${params}` : "";
};

export const subscriptionsApi = {
  getAvailablePlans: (page: number, pageSize: number) => http.get<PlanPageWireDto>(`${companyBase}/plans${query({ page, pageSize })}`).then(normalizePlanPage),
  getMySubscription: () => http.get<UserSubscriptionDto>(`${companyBase}/me`).then(normalizeCurrentSubscription),
  getCurrentUsage: () => http.get<PaygUsageWireDto>(`${companyBase}/usage/current`).then(normalizeUsage),
  getMyRequests: (page: number, pageSize: number) => http.get<PlanChangeRequestPageWireDto>(`${companyBase}/plan-change-requests${query({ page, pageSize })}`).then((result) => ({ ...result, items: result.items.map(normalizeRequest) })),
  createMyRequest: (request: CreatePlanChangeRequest) => http.post<PlanChangeRequestWireDto>(`${companyBase}/plan-change-requests`, { requestedPlanId: request.requestedPlanId, requestedBillingCycle: billingCycleValue[request.requestedBillingCycle] }).then(normalizeRequest),
  cancelMyRequest: (id: string) => http.post<PlanChangeRequestWireDto>(`${companyBase}/plan-change-requests/${encodeURIComponent(id)}/cancel`).then(normalizeRequest),

  getPlatformPlans: (filters: { page: number; pageSize: number; isActive?: boolean; search?: string }) => http.get<PlanPageWireDto>(`${platformBase}/plans${query(filters)}`).then(normalizePlanPage),
  getPlatformPlan: (id: string) => http.get<SubscriptionPlanWireDto>(`${platformBase}/plans/${encodeURIComponent(id)}`).then(normalizePlan),
  createPlatformPlan: (request: CreatePlanRequest) => http.post<SubscriptionPlanWireDto>(`${platformBase}/plans`, { ...request, pricingModel: pricingModelValue[request.pricingModel] }).then(normalizePlan),
  setPlatformPlanActive: (id: string, active: boolean) => http.post<SubscriptionPlanWireDto>(`${platformBase}/plans/${encodeURIComponent(id)}/${active ? "activate" : "deactivate"}`).then(normalizePlan),

  getPlatformSubscriptions: (filters: Record<string, string | number | boolean | undefined | null>) => http.get<SubscriptionPageWireDto>(`${platformBase}/subscriptions${query(filters)}`).then((page) => ({ ...page, items: page.items.map(normalizeSubscription) })),
  getPlatformSubscription: (id: string) => http.get<SubscriptionAdministrationWireDto>(`${platformBase}/subscriptions/${encodeURIComponent(id)}`).then(normalizeSubscription),
  createPlatformSubscription: (request: CreateCompanySubscriptionRequest) => {
    const { billingCycle, ...fields } = request;
    return http.post<SubscriptionAdministrationWireDto>(`${platformBase}/subscriptions`, { ...fields, billingCycle: billingCycleValue[billingCycle] }).then(normalizeSubscription);
  },
  getPlatformCompanies: () => http.get<PlatformCompanyListItemDto[]>(`${platformBase}/companies`),

  getPlatformRequests: (filters: Record<string, string | number | boolean | undefined | null>) => http.get<PlanChangeRequestPageWireDto>(`${platformBase}/plan-change-requests${query(filters)}`).then((result) => ({ ...result, items: result.items.map(normalizeRequest) })),
  getPlatformRequest: (id: string) => http.get<PlanChangeRequestWireDto>(`${platformBase}/plan-change-requests/${encodeURIComponent(id)}`).then(normalizeRequest),
  approvePlatformRequest: (id: string, request: ApprovePlanChangeRequestRequest) => http.post<PlanChangeRequestDto>(`${platformBase}/plan-change-requests/${encodeURIComponent(id)}/approve`, request),
  rejectPlatformRequest: (id: string, request: RejectPlanChangeRequestRequest) => http.post<PlanChangeRequestDto>(`${platformBase}/plan-change-requests/${encodeURIComponent(id)}/reject`, request),
};
