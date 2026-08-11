import { http } from "@/shared/lib/http";
import type { NotificationDto, GetMyNotificationsParams } from "@/features/notifications/types/notifications.types";

export const notificationsApi = {
  /**
   * Gets current user's in-app notification inbox (keyset-paginated).
   * GET /api/v1/notifications/me
   */
  getMyNotifications(params?: GetMyNotificationsParams): Promise<NotificationDto[]> {
    return http.get<NotificationDto[]>("/api/v1/notifications/me", { params });
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
};
