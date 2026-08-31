import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { maintenanceApi } from '../api/maintenance.api';
import { maintenanceKeys } from './maintenanceKeys';
import {
  CreateMaintenanceRequestRequest,
  UpdateMaintenanceRequestRequest,
  UpdateMaintenanceRequestStatusRequest,
  AddMaintenanceCommentRequest,
  EditMaintenanceCommentRequest,
  AddMaintenanceAttachmentRequest,
  MaintenanceRequestFilterOptions,
} from '../types/maintenance.types';
import { toast } from 'sonner';
import { useTranslation } from '@/shared/i18n';

export const useMaintenanceRequests = (filters?: MaintenanceRequestFilterOptions) => {
  return useQuery({
    queryKey: maintenanceKeys.list(filters || {}),
    queryFn: () => maintenanceApi.getRequests(filters),
  });
};

export const useMaintenanceRequest = (id: string, enabled = true) => {
  return useQuery({
    queryKey: maintenanceKeys.detail(id),
    queryFn: () => maintenanceApi.getById(id),
    enabled: !!id && enabled,
  });
};

export const useMaintenanceComments = (id: string, enabled = true) => {
  return useQuery({
    queryKey: maintenanceKeys.comments(id),
    queryFn: () => maintenanceApi.getComments(id),
    enabled: !!id && enabled,
  });
};

export const useMaintenanceAttachments = (id: string, enabled = true) => {
  return useQuery({
    queryKey: maintenanceKeys.attachments(id),
    queryFn: () => maintenanceApi.getAttachments(id),
    enabled: !!id && enabled,
  });
};

export const useMaintenanceHistory = (id: string, enabled = true) => {
  return useQuery({
    queryKey: maintenanceKeys.history(id),
    queryFn: () => maintenanceApi.getStatusHistory(id),
    enabled: !!id && enabled,
  });
};

export const useCreateMaintenanceRequest = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateMaintenanceRequestRequest) => maintenanceApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.lists() });
      toast.success(t('maintenance.createSuccess'));
    },
    onError: () => {
      toast.error(t('maintenance.createFailed'));
    },
  });
};

export const useUpdateMaintenanceRequest = (id: string) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateMaintenanceRequestRequest) => maintenanceApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.lists() });
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.detail(id) });
      toast.success(t('maintenance.updateSuccess'));
    },
    onError: () => {
      toast.error(t('maintenance.updateFailed'));
    },
  });
};

export const useUpdateMaintenanceStatus = (id: string) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateMaintenanceRequestStatusRequest) => maintenanceApi.updateStatus(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.lists() });
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.history(id) });
      toast.success(t('maintenance.statusUpdated'));
    },
    onError: () => {
      toast.error(t('maintenance.statusUpdateFailed'));
    },
  });
};

export const useAddMaintenanceComment = (id: string) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: AddMaintenanceCommentRequest) => maintenanceApi.addComment(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.comments(id) });
      toast.success(t('maintenance.commentAdded'));
    },
    onError: () => {
      toast.error(t('maintenance.commentAddFailed'));
    },
  });
};

export const useRemoveMaintenanceComment = (id: string) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (commentId: string) => maintenanceApi.removeComment(id, commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.comments(id) });
      toast.success(t('maintenance.commentRemoved'));
    },
    onError: () => {
      toast.error(t('maintenance.commentRemoveFailed'));
    },
  });
};

export const useAddMaintenanceAttachment = (id: string) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: AddMaintenanceAttachmentRequest) => maintenanceApi.addAttachment(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.attachments(id) });
      toast.success(t('maintenance.attachmentAdded'));
    },
    onError: () => {
      toast.error(t('maintenance.attachmentAddFailed'));
    },
  });
};

export const useRemoveMaintenanceAttachment = (id: string) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (attachmentId: string) => maintenanceApi.removeAttachment(id, attachmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.attachments(id) });
      toast.success(t('maintenance.attachmentRemoved'));
    },
    onError: () => {
      toast.error(t('maintenance.attachmentRemoveFailed'));
    },
  });
};
