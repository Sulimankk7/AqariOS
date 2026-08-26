import { http } from "@/shared/lib/http";
import type { KeysetPage, LinkUtilityAccountRequest, ManagementUtilityAccountDto, ReplaceUtilityAccountRequest, UtilityAccountFilters, UtilityBillDto, UtilityBillPaymentStatus } from "../types/utilityBills.types";

const ACCOUNTS = "/api/v1/utility-bills/accounts";

function withQuery(path: string, values: Record<string, string | number | boolean | undefined>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined) params.set(key, String(value));
  });
  const query = params.toString();
  return query ? `${path}?${query}` : path;
}

export const utilityBillsApi = {
  getAccounts(filters: UtilityAccountFilters, cursor?: string, pageSize = 25) {
    return http.get<KeysetPage<ManagementUtilityAccountDto>>(withQuery(ACCOUNTS, { ...filters, cursor, pageSize }));
  },
  getAccount(id: string) {
    return http.get<ManagementUtilityAccountDto>(`${ACCOUNTS}/${id}`);
  },
  linkAccount(request: LinkUtilityAccountRequest) {
    return http.post<string>(ACCOUNTS, request);
  },
  unlinkAccount(id: string) {
    return http.delete<void>(`${ACCOUNTS}/${id}`);
  },
  replaceAccount(id: string, request: ReplaceUtilityAccountRequest) {
    return http.post<string>(`${ACCOUNTS}/${id}/replace`, request);
  },
  requestSync(id: string) {
    return http.post<void>(`${ACCOUNTS}/${id}/sync`);
  },
  getBills(id: string, paymentStatus?: UtilityBillPaymentStatus, cursor?: string, pageSize = 25) {
    return http.get<KeysetPage<UtilityBillDto>>(withQuery(`${ACCOUNTS}/${id}/bills`, { paymentStatus: paymentStatus === undefined ? undefined : String(paymentStatus), cursor, pageSize }));
  },
};
