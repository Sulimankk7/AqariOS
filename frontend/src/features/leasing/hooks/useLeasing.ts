import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { leasingApi } from '../api/leasing.api';
import { leaseKeys } from './leaseKeys';
import {
  CreateLeaseContractRequest,
  UpdateDraftLeaseContractRequest,
  RenewLeaseContractRequest,
  TerminateLeaseContractRequest,
  AttachContractDocumentRequest,
} from '../types/leasing.types';
import { toast } from 'sonner';
import { extractUserFriendlyError } from '@/shared/utils';
import { getLeasingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

function useT() {
  const { language } = useTranslation();
  return (key: string) => getLeasingTranslation(key, language);
}

export const useSearchLeases = (searchTerm: string = '', pageSize: number = 50) => {
  return useQuery({
    queryKey: leaseKeys.search(searchTerm, pageSize),
    queryFn: () => leasingApi.searchContracts(searchTerm, pageSize),
  });
};

export const useExpiringLeases = (daysAhead: number = 30) => {
  return useQuery({
    queryKey: leaseKeys.expiring(daysAhead),
    queryFn: () => leasingApi.getExpiringContracts(daysAhead),
  });
};

export const useApartmentLeaseHistory = (apartmentId: string, pageSize: number = 50) => {
  return useQuery({
    queryKey: leaseKeys.apartmentHistory(apartmentId, pageSize),
    queryFn: () => leasingApi.getApartmentLeaseHistory(apartmentId, pageSize),
    enabled: !!apartmentId,
  });
};

export const useLeaseDetails = (id: string) => {
  return useQuery({
    queryKey: leaseKeys.detail(id),
    queryFn: () => leasingApi.getContractById(id),
    enabled: !!id,
  });
};

export const useTenantsLookup = (searchTerm: string = '') => {
  return useQuery({
    queryKey: leaseKeys.tenants(searchTerm),
    queryFn: () => leasingApi.getTenants(searchTerm),
  });
};

export const useCreateLease = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: (data: CreateLeaseContractRequest) => leasingApi.createContract(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.all });
      toast.success(t('createSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useUpdateDraftLease = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDraftLeaseContractRequest }) =>
      leasingApi.updateDraftContract(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.all });
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(variables.id) });
      toast.success(t('updateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useActivateLease = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: (id: string) => leasingApi.activateContract(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.all });
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(id) });
      toast.success(t('activateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useTerminateLease = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: TerminateLeaseContractRequest }) =>
      leasingApi.terminateContract(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.all });
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: ['parking'] });
      queryClient.invalidateQueries({ queryKey: ['parking-assignment'] });
      queryClient.invalidateQueries({ queryKey: ['lease-parking', variables.id] });
      toast.success(t('terminateSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useRenewLease = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: RenewLeaseContractRequest }) =>
      leasingApi.renewContract(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.all });
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(variables.id) });
      toast.success(t('renewSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useAttachContractDocument = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AttachContractDocumentRequest }) =>
      leasingApi.attachDocument(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(variables.id) });
      toast.success(t('attachSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useReplaceContractDocument = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: ({ contractId, documentId, data }: { contractId: string; documentId: string; data: { newFileId: string; description?: string | null } }) =>
      leasingApi.replaceDocument(contractId, documentId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(variables.contractId) });
      toast.success(t('docReplacedSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};

export const useDeleteContractDocument = () => {
  const queryClient = useQueryClient();
  const t = useT();

  return useMutation({
    mutationFn: ({ contractId, documentId }: { contractId: string; documentId: string }) =>
      leasingApi.deleteDocument(contractId, documentId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: leaseKeys.detail(variables.contractId) });
      toast.success(t('docDeletedSuccess'));
    },
    onError: (error: any) => {
      if (!error?.validationErrors || Object.keys(error.validationErrors).length === 0) {
        toast.error(extractUserFriendlyError(error, t('actionFailed')));
      }
    },
  });
};
