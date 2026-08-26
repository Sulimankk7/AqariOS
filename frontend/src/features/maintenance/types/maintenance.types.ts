export enum MaintenanceCategory {
  Electrical = 0,
  Plumbing = 1,
  AirConditioning = 2,
  Elevator = 3,
  Cleaning = 4,
  Water = 5,
  Structural = 6,
  DoorsWindows = 7,
  Internet = 8,
  Other = 9,
}

export enum MaintenancePriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Emergency = 3,
}

export enum MaintenanceStatus {
  Open = 0,
  InProgress = 1,
  Waiting = 2,
  Resolved = 3,
  Closed = 4,
  Cancelled = 5,
}

export interface MaintenanceRequestSummaryDto {
  id: string;
  companyId: string;
  buildingId: string;
  apartmentId?: string;
  tenantId?: string;
  title: string;
  category: MaintenanceCategory;
  priority: MaintenancePriority;
  status: MaintenanceStatus;
  requestDate: string; // DateOnly serialized as string YYYY-MM-DD
  closedDate?: string;
  createdBy?: string;
  createdAt: string; // DateTimeOffset
}

export interface MaintenanceRequestDetailDto {
  id: string;
  companyId: string;
  buildingId: string;
  apartmentId?: string;
  tenantId?: string;
  title: string;
  description: string;
  category: MaintenanceCategory;
  priority: MaintenancePriority;
  status: MaintenanceStatus;
  requestDate: string;
  closedDate?: string;
  internalNotes?: string;
  attachmentCount: number;
  commentCount: number;
  createdBy?: string;
  createdAt: string;
  updatedBy?: string;
  updatedAt: string;
}

export interface MaintenanceAttachmentDto {
  id: string;
  fileId?: string;
  description?: string;
  uploadedBy?: string;
  createdBy?: string;
  createdAt: string;
}

export interface MaintenanceCommentDto {
  id: string;
  commentText: string;
  createdBy?: string;
  createdAt: string;
  updatedBy?: string;
  updatedAt: string;
}

export interface MaintenanceStatusHistoryDto {
  id: string;
  previousStatus?: MaintenanceStatus;
  newStatus: MaintenanceStatus;
  changedBy?: string;
  changedAt: string;
  reason?: string;
}

export interface CreateMaintenanceRequestRequest {
  buildingId: string;
  title: string;
  description: string;
  category: MaintenanceCategory;
  priority: MaintenancePriority;
  requestDate: string;
  apartmentId?: string;
  tenantId?: string;
}

export interface UpdateMaintenanceRequestRequest {
  buildingId: string;
  title: string;
  description: string;
  category: MaintenanceCategory;
  priority: MaintenancePriority;
  apartmentId?: string;
  tenantId?: string;
  internalNotes?: string;
}

export interface UpdateMaintenanceRequestStatusRequest {
  newStatus: MaintenanceStatus;
  reason?: string;
}

export interface AddMaintenanceCommentRequest {
  commentText: string;
}

export interface EditMaintenanceCommentRequest {
  newText: string;
}

export interface AddMaintenanceAttachmentRequest {
  fileId: string;
  description?: string;
}

export interface MaintenanceRequestFilterOptions {
  buildingId?: string;
  apartmentId?: string;
  tenantId?: string;
  status?: MaintenanceStatus;
  priority?: MaintenancePriority;
  category?: MaintenanceCategory;
  dateFrom?: string;
  dateTo?: string;
  searchText?: string;
  lastSeenId?: string;
  lastSeenRequestDate?: string;
  pageSize?: number;
}
