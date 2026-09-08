import { http } from "@/shared/lib/http";
import type { ContactRequestDetail, ContactRequestPage, ContactRequestStatus } from "../types/contactRequests.types";
const base = "/api/v1/platform/contact-requests";
export const platformContactRequestsApi = {
  list(page: number, pageSize: number, status?: string) { const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) }); if (status) query.set("status", status); return http.get<ContactRequestPage>(`${base}?${query}`); },
  detail(id: string) { return http.get<ContactRequestDetail>(`${base}/${encodeURIComponent(id)}`); },
  updateStatus(id: string, status: ContactRequestStatus) { return http.patch<ContactRequestDetail>(`${base}/${encodeURIComponent(id)}/status`, { status }); },
};
