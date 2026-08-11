import { http } from "@/shared/lib/http";
import type { TenantDetailDto } from "@/features/tenantPortal/types/tenantPortal.types";

export const tenantPortalApi = {
  /**
   * Gets complete detail profile for currently authenticated Tenant session.
   * GET /api/v1/tenant-portal/me
   */
  getProfile(): Promise<TenantDetailDto> {
    return http.get<TenantDetailDto>("/api/v1/tenant-portal/me");
  },
};
