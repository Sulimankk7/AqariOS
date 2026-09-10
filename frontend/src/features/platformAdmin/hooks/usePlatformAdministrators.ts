import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { platformAdminApi } from "../api/platformAdmin.api";

const administratorsKey = ["platform", "administrators"] as const;

export function usePlatformAdministrators() {
  return useQuery({
    queryKey: administratorsKey,
    queryFn: () => platformAdminApi.getAdministrators(),
  });
}

export function useCreatePlatformAdministrator() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: platformAdminApi.createAdministrator,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: administratorsKey }),
  });
}
