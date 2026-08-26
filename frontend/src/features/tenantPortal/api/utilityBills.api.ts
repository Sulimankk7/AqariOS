import { http } from "@/shared/lib/http";
import type {
  TenantLinkUtilityAccountRequest,
  TenantReplaceUtilityAccountRequest,
  KeysetPage,
  TenantUtilityAccountDto,
  TenantUtilityBillDto,
  TenantUtilityDashboardSummaryDto,
  TenantUtilityTypeName,
} from "../types/utilityBills.types";

const MY_UTILITY_BILLS = "/api/v1/utility-bills/my";

function withQuery(path: string, values: Record<string, string | number | undefined>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined) params.set(key, String(value));
  });
  const query = params.toString();
  return query ? `${path}?${query}` : path;
}

export const tenantUtilityBillsApi = {
  getAccounts() {
    return http.get<TenantUtilityAccountDto[]>(`${MY_UTILITY_BILLS}/accounts`);
  },
  linkAccount(request: TenantLinkUtilityAccountRequest) {
    return http.post<TenantUtilityAccountDto>(`${MY_UTILITY_BILLS}/accounts`, request);
  },
  replaceAccount(id: string, request: TenantReplaceUtilityAccountRequest) {
    return http.post<TenantUtilityAccountDto>(`${MY_UTILITY_BILLS}/accounts/${id}/replace`, request);
  },
  unlinkAccount(id: string) {
    return http.delete<void>(`${MY_UTILITY_BILLS}/accounts/${id}`);
  },
  requestSync(id: string) {
    return http.post<void>(`${MY_UTILITY_BILLS}/accounts/${id}/sync`);
  },
  getBills(utilityType?: TenantUtilityTypeName, pageSize = 50, cursor?: string) {
    return http.get<KeysetPage<TenantUtilityBillDto>>(
      withQuery(`${MY_UTILITY_BILLS}/bills`, { utilityType, pageSize, cursor }),
    );
  },
  getDashboardSummary() {
    return http.get<TenantUtilityDashboardSummaryDto>(`${MY_UTILITY_BILLS}/dashboard-summary`);
  },
};
