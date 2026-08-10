import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tenantsApi } from '../api/tenants.api';
import { tenantKeys } from './tenantKeys';
import { CreateTenantVehicleRequest, UpdateTenantVehicleRequest } from '../types/tenants.types';
import { toast } from 'sonner';
import { extractUserFriendlyError } from '@/shared/utils';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

function useTenantT() {
  const { language } = useTranslation();
  return (key: string) => getTenantTranslation(key, language);
}

export const useVehicles = (tenantId: string) => {
  return useQuery({
    queryKey: tenantKeys.vehicles(tenantId),
    queryFn: () => tenantsApi.getVehicles(tenantId),
    enabled: !!tenantId,
  });
};

export const useCreateVehicle = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (data: CreateTenantVehicleRequest) => tenantsApi.createVehicle(tenantId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.vehicles(tenantId) });
      toast.success(t('vehicleCreateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useUpdateVehicle = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: ({ vehicleId, data }: { vehicleId: string; data: UpdateTenantVehicleRequest }) =>
      tenantsApi.updateVehicle(tenantId, vehicleId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.vehicles(tenantId) });
      toast.success(t('vehicleUpdateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useDeleteVehicle = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (vehicleId: string) => tenantsApi.deleteVehicle(tenantId, vehicleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.vehicles(tenantId) });
      toast.success(t('vehicleDeleteSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    },
  });
};
