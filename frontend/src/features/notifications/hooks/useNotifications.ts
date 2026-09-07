import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notificationsApi } from "@/features/notifications/api/notifications.api";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ApiError } from "@/shared/lib/http";
import type { GetMyNotificationsParams } from "@/features/notifications/types/notifications.types";

/**
 * Hook to fetch unread notification count with tenant/user cache isolation.
 */
export function useUnreadNotificationCount({ poll = false }: { poll?: boolean } = {}) {
  const { user, isAuthenticated } = useAuth();
  const userId = user?.id;

  return useQuery({
    queryKey: ["tenant", userId, "notifications", user?.companyId, "unread-count"],
    queryFn: () => notificationsApi.getUnreadCount(),
    enabled: isAuthenticated && !!userId,
    staleTime: 30 * 1000, // 30 seconds stale time
    refetchInterval: poll ? 60 * 1000 : false,
    retry: (failureCount, error) =>
      failureCount < 2 && (!(error instanceof ApiError) || error.status >= 500),
  });
}

/**
 * Hook to fetch user's notification list with tenant/user cache isolation.
 */
export function useMyNotifications(params?: GetMyNotificationsParams) {
  const { user, isAuthenticated } = useAuth();
  const userId = user?.id;

  return useQuery({
    queryKey: ["tenant", userId, "notifications", user?.companyId, "list", params],
    queryFn: () => notificationsApi.getMyNotifications(params),
    enabled: isAuthenticated && !!userId,
    staleTime: 15 * 1000,
    retry: false,
  });
}

/**
 * Hook to mark a notification as read and invalidate unread count & notification list cache.
 */
export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const userId = user?.id;

  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: () => {
      // Invalidate isolated query keys
      queryClient.invalidateQueries({
        queryKey: ["tenant", userId, "notifications"],
      });
    },
  });
}

/**
 * Hook to mark all unread notifications for current user as read.
 * Invalidates user/tenant notifications cache upon success.
 */
export function useMarkAllNotificationsAsRead() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const userId = user?.id;

  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["tenant", userId, "notifications"],
      });
    },
  });
}
