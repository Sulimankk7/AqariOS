import { http } from '@/shared/lib/http';
import {
  MaintenanceRequestSummaryDto,
  MaintenanceRequestDetailDto,
  MaintenanceAttachmentDto,
  MaintenanceCommentDto,
  MaintenanceStatusHistoryDto,
  CreateMaintenanceRequestRequest,
  UpdateMaintenanceRequestRequest,
  UpdateMaintenanceRequestStatusRequest,
  AddMaintenanceCommentRequest,
  EditMaintenanceCommentRequest,
  AddMaintenanceAttachmentRequest,
  MaintenanceRequestFilterOptions,
} from '../types/maintenance.types';

const BASE_PATH = '/api/v1.0/maintenance-requests';

export const maintenanceApi = {
  getRequests: (params?: MaintenanceRequestFilterOptions): Promise<MaintenanceRequestSummaryDto[]> => {
    return http.get<MaintenanceRequestSummaryDto[]>(BASE_PATH, { params });
  },

  getById: (id: string): Promise<MaintenanceRequestDetailDto> => {
    return http.get<MaintenanceRequestDetailDto>(`${BASE_PATH}/${id}`);
  },

  create: (data: CreateMaintenanceRequestRequest): Promise<string> => {
    return http.post<string>(BASE_PATH, data);
  },

  update: (id: string, data: UpdateMaintenanceRequestRequest): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${id}`, data);
  },

  updateStatus: (id: string, data: UpdateMaintenanceRequestStatusRequest): Promise<void> => {
    return http.patch<void>(`${BASE_PATH}/${id}/status`, data);
  },

  delete: (id: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${id}`);
  },

  getComments: (id: string): Promise<MaintenanceCommentDto[]> => {
    return http.get<MaintenanceCommentDto[]>(`${BASE_PATH}/${id}/comments`);
  },

  addComment: (id: string, data: AddMaintenanceCommentRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${id}/comments`, data);
  },

  editComment: (id: string, commentId: string, data: EditMaintenanceCommentRequest): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${id}/comments/${commentId}`, data);
  },

  removeComment: (id: string, commentId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${id}/comments/${commentId}`);
  },

  getAttachments: (id: string): Promise<MaintenanceAttachmentDto[]> => {
    return http.get<MaintenanceAttachmentDto[]>(`${BASE_PATH}/${id}/attachments`);
  },

  addAttachment: (id: string, data: AddMaintenanceAttachmentRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${id}/attachments`, data);
  },

  removeAttachment: (id: string, attachmentId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${id}/attachments/${attachmentId}`);
  },

  getStatusHistory: (id: string): Promise<MaintenanceStatusHistoryDto[]> => {
    return http.get<MaintenanceStatusHistoryDto[]>(`${BASE_PATH}/${id}/status-history`);
  },
};
