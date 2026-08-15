import { useQuery } from "@tanstack/react-query";
import { tenantPortalApi } from "@/features/tenantPortal/api/tenantPortal.api";
import { useAuth } from "@/features/auth/hooks/useAuth";

/**
 * Custom hook to fetch the currently authenticated tenant's active lease contract.
 * Uses strict tenant/user cache isolation key: ["tenant", userId, "my-lease"].
 */
export function useTenantLease() {
  const { user, isAuthenticated } = useAuth();
  const userId = user?.id;

  return useQuery({
    queryKey: ["tenant", userId, "my-lease"],
    queryFn: () => tenantPortalApi.getMyLease(),
    enabled: isAuthenticated && !!userId,
    staleTime: 5 * 60 * 1000, // 5 minutes stale time for lease data
    gcTime: 15 * 60 * 1000, // 15 minutes cache garbage collection time
    retry: (failureCount, error) => {
      // Don't retry client 404 or authorization failures
      return failureCount < 2;
    },
  });
}
