import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tenantsApi } from '../api/tenants.api';
import { tenantKeys } from './tenantKeys';
import { CreateTenantEmergencyContactRequest, UpdateTenantEmergencyContactRequest } from '../types/tenants.types';
import { toast } from 'sonner';
import { extractUserFriendlyError } from '@/shared/utils';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

function useTenantT() {
  const { language } = useTranslation();
  return (key: string) => getTenantTranslation(key, language);
}

export const useEmergencyContacts = (tenantId: string) => {
  return useQuery({
    queryKey: tenantKeys.emergencyContacts(tenantId),
    queryFn: () => tenantsApi.getEmergencyContacts(tenantId),
    enabled: !!tenantId,
  });
};

export const useCreateEmergencyContact = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (data: CreateTenantEmergencyContactRequest) => tenantsApi.createEmergencyContact(tenantId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.emergencyContacts(tenantId) });
      toast.success(t('emergencyContactCreateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useUpdateEmergencyContact = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: ({ contactId, data }: { contactId: string; data: UpdateTenantEmergencyContactRequest }) =>
      tenantsApi.updateEmergencyContact(tenantId, contactId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.emergencyContacts(tenantId) });
      toast.success(t('emergencyContactUpdateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useDeleteEmergencyContact = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (contactId: string) => tenantsApi.deleteEmergencyContact(tenantId, contactId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.emergencyContacts(tenantId) });
      toast.success(t('emergencyContactDeleteSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    },
  });
};
