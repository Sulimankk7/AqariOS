import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apartmentsApi } from '../api/apartments.api';
import { apartmentKeys } from './apartmentKeys';
import { 
  CreateApartmentRequest, 
  UpdateApartmentRequest, 
  ListApartmentsParams 
} from '../types/apartments.types';
import { ApartmentFormValues } from '../schemas/apartments.schema';
import { toast } from 'sonner';
import { getApartmentTranslation } from '../constants/translations';
import { isArchiveBlockedError } from '@/shared/lib/archiveBlocked';
import { getRuntimeLanguage } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils/errorHandling';

const t = (key: string) => getApartmentTranslation(key, getRuntimeLanguage());

export const useApartments = (params?: ListApartmentsParams) => {
  return useQuery({
    queryKey: apartmentKeys.list(params),
    queryFn: () => apartmentsApi.getApartments(params),
  });
};

export const useApartment = (id: string) => {
  return useQuery({
    queryKey: apartmentKeys.detail(id),
    queryFn: () => apartmentsApi.getApartmentById(id),
    enabled: !!id,
  });
};

export const useCreateApartment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ floorId, data }: { floorId: string; data: ApartmentFormValues | CreateApartmentRequest }) => 
      apartmentsApi.createApartment(floorId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apartmentKeys.all });
      toast.success(t('createSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    }
  });
};

export const useUpdateApartment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ApartmentFormValues | UpdateApartmentRequest }) => 
      apartmentsApi.updateApartment(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: apartmentKeys.all });
      queryClient.invalidateQueries({ queryKey: apartmentKeys.detail(variables.id) });
      toast.success(t('updateSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    }
  });
};

export const useDeleteApartment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => apartmentsApi.deleteApartment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apartmentKeys.all });
      toast.success(t('deleteSuccess'));
    },
    onError: (error: any) => {
      if (isArchiveBlockedError(error)) {
        return;
      }
      toast.error(extractUserFriendlyError(error, t('loadError')));
    }
  });
};
