export function canMarkRead(notification: {
  status: number;
  readAt?: string | null;
}): boolean {
  return notification.status === 1 && !notification.readAt;
}
export const notificationTypeKeys = [
  "newLease",
  "leaseExpiration",
  "rentDue",
  "rentPaid",
  "latePayment",
  "maintenanceCreated",
  "maintenanceUpdated",
  "marketplace",
  "documentExpiring",
  "general",
  "electricity",
  "water",
];
