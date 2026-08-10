import { http } from "@/shared/lib/http";
import type { UserProfileDto } from "@/features/auth/types/auth.types";

export const tenantPortalApi = {
  /**
   * Gets user profile context for authenticated Tenant session.
   * GET /api/v1/tenant-portal/me
   */
  getProfile(): Promise<UserProfileDto> {
    return http.get<UserProfileDto>("/api/v1/tenant-portal/me");
  },
};
