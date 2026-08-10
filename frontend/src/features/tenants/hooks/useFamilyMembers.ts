import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tenantsApi } from '../api/tenants.api';
import { tenantKeys } from './tenantKeys';
import { CreateTenantFamilyMemberRequest, UpdateTenantFamilyMemberRequest } from '../types/tenants.types';
import { toast } from 'sonner';
import { extractUserFriendlyError } from '@/shared/utils';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

function useTenantT() {
  const { language } = useTranslation();
  return (key: string) => getTenantTranslation(key, language);
}

export const useFamilyMembers = (tenantId: string) => {
  return useQuery({
    queryKey: tenantKeys.familyMembers(tenantId),
    queryFn: () => tenantsApi.getFamilyMembers(tenantId),
    enabled: !!tenantId,
  });
};

export const useCreateFamilyMember = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (data: CreateTenantFamilyMemberRequest) => tenantsApi.createFamilyMember(tenantId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.familyMembers(tenantId) });
      toast.success(t('familyMemberCreateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useUpdateFamilyMember = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: ({ familyMemberId, data }: { familyMemberId: string; data: UpdateTenantFamilyMemberRequest }) =>
      tenantsApi.updateFamilyMember(tenantId, familyMemberId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.familyMembers(tenantId) });
      toast.success(t('familyMemberUpdateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('loadError')));
      }
    },
  });
};

export const useDeleteFamilyMember = (tenantId: string) => {
  const queryClient = useQueryClient();
  const t = useTenantT();

  return useMutation({
    mutationFn: (familyMemberId: string) => tenantsApi.deleteFamilyMember(tenantId, familyMemberId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tenantKeys.detail(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantKeys.familyMembers(tenantId) });
      toast.success(t('familyMemberDeleteSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    },
  });
};
