import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tenantsApi } from '../api/tenants.api';
import { tenantKeys } from './tenantKeys';
import { CreateTenantRequest, UpdateTenantRequest, ProvisionTenantAccountRequest } from '../types/tenants.types';
import { toast } from 'sonner';
import { extractUserFriendlyError } from '@/shared/utils';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

function useTenantT() {
  const { language } = useTranslation();
  return (key: string) => getTenantTranslation(key, language);
}

export const useSearchTenants = (searchTerm: string = '') => {
  return useQuery({
    queryKey: tenantKeys.search(searchTerm),
    queryFn: () => tenantsApi.searchTenants(searchTerm),
  });
};

export const useTenantDetails = (tenantId: string) => {
  return useQuery({
    queryKey: tenantKeys.detail(tenantId),
    queryFn: () => tenantsApi.getTenantById(tenantId),
    enabled: !!tenantId,
  });
};

export const useTenantLeaseHistory = (tenantId: string, pageSize: number = 50) => {
  return useQuery({
    queryKey: tenantKeys.leases(tenantId, pageSize),
    queryFn: () => tenantsApi.getLeaseHistoryByTenant(tenantId, pageSize),
    enabled: !!tenantId,
  });
};

export const useCreateTenant = () => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (data: CreateTenantRequest) => tenantsApi.createTenant(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.all });
      toast.success(t('createSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useUpdateTenant = () => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTenantRequest }) =>
      tenantsApi.updateTenant(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.all });
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(variables.id) });
      toast.success(t('updateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useDeleteTenant = () => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (id: string) => tenantsApi.deleteTenant(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.all });
      toast.success(t('deleteSuccess'));
    },
    onError: (error: any) => {
      // Show user friendly message on failure (e.g. HTTP 422 Conflict when tenant has active leases)
      const message = extractUserFriendlyError(error, t('deleteFailedCannotDeleteActiveLease'));
      toast.error(message);
    },
  });
};

export const useProvisionTenantAccount = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ tenantId, ...data }: { tenantId: string } & ProvisionTenantAccountRequest) =>
      tenantsApi.provisionAccount(tenantId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(variables.tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.all });
    },
  });
};
