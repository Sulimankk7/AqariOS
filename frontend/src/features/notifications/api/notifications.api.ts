import { http } from "@/shared/lib/http";
import type { NotificationDto, GetMyNotificationsParams, MarkAllNotificationsAsReadResult } from "@/features/notifications/types/notifications.types";

export const notificationsApi = {
  /**
   * Gets current user's in-app notification inbox (keyset-paginated).
   * GET /api/v1/notifications/me
   */
  getMyNotifications(params?: GetMyNotificationsParams): Promise<NotificationDto[]> {
    const query = new URLSearchParams();
    if (params?.pageSize !== undefined) query.set('pageSize', String(params.pageSize));
    if (params?.lastSeenCreatedAt && params?.lastSeenId) {
      query.set('lastSeenCreatedAt', params.lastSeenCreatedAt);
      query.set('lastSeenId', params.lastSeenId);
    }
    return http.get<NotificationDto[]>(`/api/v1/notifications/me${query.size ? `?${query}` : ''}`);
  },

  /**
   * Gets current user's unread notification count.
   * GET /api/v1/notifications/me/unread-count
   */
  getUnreadCount(): Promise<number> {
    return http.get<number>("/api/v1/notifications/me/unread-count");
  },

  /**
   * Marks a notification as read by its recipient.
   * PATCH /api/v1/notifications/{id}/read
   */
  markAsRead(id: string): Promise<void> {
    return http.patch<void>(`/api/v1/notifications/${id}/read`, {});
  },

  /**
   * Marks all unread notifications for current user as read.
   * PATCH /api/v1/notifications/me/read-all
   */
  markAllAsRead(): Promise<MarkAllNotificationsAsReadResult> {
    return http.patch<MarkAllNotificationsAsReadResult>("/api/v1/notifications/me/read-all", {});
  },
};
