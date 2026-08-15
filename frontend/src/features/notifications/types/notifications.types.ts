/**
 * Notifications Types — Synchronized with ASP.NET Core Backend DTOs.
 * Source DTO: PropertyOS.Application.Notifications.Queries.Common.NotificationDto
 */

export enum NotificationType {
  General = 0,
  LeaseExpiry = 1,
  PaymentDue = 2,
  PaymentOverdue = 3,
  MaintenanceUpdate = 4,
  SystemAlert = 5,
}

export enum NotificationPriority {
  Low = 0,
  Normal = 1,
  High = 2,
  Urgent = 3,
}

export enum NotificationStatus {
  Unread = 0,
  Read = 1,
}

export interface NotificationDto {
  id: string;
  recipientUserId: string;
  subject: string;
  body: string;
  notificationType: NotificationType;
  priority: NotificationPriority;
  status: NotificationStatus;
  createdAt: string;
  readAt?: string | null;
}

export interface GetMyNotificationsParams {
  lastSeenCreatedAt?: string;
  lastSeenId?: string;
  pageSize?: number;
}

export interface MarkAllNotificationsAsReadResult {
  markedCount: number;
}

