import { http } from "@/shared/lib/http";
import type {
  LandlordRegistrationDetailDto,
  LandlordRegistrationPageDto,
  LandlordRegistrationReviewResultDto,
  RejectLandlordRegistrationRequest,
} from "../types/platformAdmin.types";

const base = "/api/v1/platform/landlord-registrations";

export const platformAdminApi = {
  getPending(page: number, pageSize: number) {
    return http.get<LandlordRegistrationPageDto>(`${base}/pending?page=${page}&pageSize=${pageSize}`);
  },
  getRegistration(registrationId: string) {
    return http.get<LandlordRegistrationDetailDto>(`${base}/${encodeURIComponent(registrationId)}`);
  },
  approve(registrationId: string) {
    return http.post<LandlordRegistrationReviewResultDto>(`${base}/${encodeURIComponent(registrationId)}/approve`);
  },
  reject(registrationId: string, request: RejectLandlordRegistrationRequest) {
    return http.post<LandlordRegistrationReviewResultDto>(`${base}/${encodeURIComponent(registrationId)}/reject`, request);
  },
};
