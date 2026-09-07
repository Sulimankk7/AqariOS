/**
 * Notifications Types — Synchronized with ASP.NET Core Backend DTOs.
 * Source DTO: PropertyOS.Application.Notifications.Queries.Common.NotificationDto
 */

export enum NotificationType {
  NewLease = 0,
  LeaseExpiration = 1,
  RentDue = 2,
  RentPaid = 3,
  LatePayment = 4,
  MaintenanceRequestCreated = 5,
  MaintenanceRequestUpdated = 6,
  MarketplaceViewingRequest = 7,
  DocumentExpiring = 8,
  GeneralNotification = 9,
  UtilityBillElectricity = 10,
  UtilityBillWater = 11,
}

export enum NotificationPriority {
  Low = 0,
  Normal = 1,
  High = 2,
  Critical = 3,
}

export enum NotificationStatus {
  Pending = 0,
  Sent = 1,
  Failed = 2,
  Cancelled = 3,
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
