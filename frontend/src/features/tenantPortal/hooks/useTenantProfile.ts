import { useQuery } from "@tanstack/react-query";
import { tenantPortalApi } from "@/features/tenantPortal/api/tenantPortal.api";
import { useAuth } from "@/features/auth/hooks/useAuth";

/**
 * Custom hook to fetch the currently authenticated tenant's authoritative profile.
 * Uses strict tenant/user cache isolation key: ["tenant", userId, "profile"].
 */
export function useTenantProfile() {
  const { user, isAuthenticated } = useAuth();
  const userId = user?.id;
  const isTenant = user?.roleCode === "TENANT";

  return useQuery({
    queryKey: ["tenant", userId, "profile"],
    queryFn: () => tenantPortalApi.getProfile(),
    enabled: isAuthenticated && isTenant && !!userId,
    staleTime: 5 * 60 * 1000,
    gcTime: 15 * 60 * 1000,
  });
}
