import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { floorsApi } from '../api/floors.api';
import { CreateFloorRequest, UpdateFloorRequest } from '../types/floors.types';
import { FloorFormValues } from '../schemas/floors.schema';
import { toast } from 'sonner';
import { getFloorTranslation } from '../constants/translations';
import { buildingKeys } from '@/features/buildings/hooks/buildingKeys';
import { isArchiveBlockedError } from '@/shared/lib/archiveBlocked';
import { getRuntimeLanguage } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils/errorHandling';

const t = (key: string) => getFloorTranslation(key, getRuntimeLanguage());

export const floorKeys = {
  all: ['floors'] as const,
  byBuilding: (buildingId: string) => [...floorKeys.all, 'building', buildingId] as const,
  detail: (id: string) => [...floorKeys.all, 'detail', id] as const,
};

export const useFloors = (buildingId?: string) => {
  return useQuery({
    queryKey: floorKeys.byBuilding(buildingId || ''),
    queryFn: () => floorsApi.getFloorsByBuilding(buildingId!),
    enabled: !!buildingId,
  });
};

export const useFloor = (id?: string) => {
  return useQuery({
    queryKey: floorKeys.detail(id || ''),
    queryFn: () => floorsApi.getFloorById(id!),
    enabled: !!id,
  });
};

export const useCreateFloor = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ buildingId, data }: { buildingId: string; data: FloorFormValues | CreateFloorRequest }) =>
      floorsApi.createFloor(buildingId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: floorKeys.all });
      queryClient.invalidateQueries({ queryKey: buildingKeys.detail(variables.buildingId) });
      toast.success(t('createSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    },
  });
};

export const useUpdateFloor = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: FloorFormValues | UpdateFloorRequest }) =>
      floorsApi.updateFloor(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: floorKeys.all });
      queryClient.invalidateQueries({ queryKey: floorKeys.detail(variables.id) });
      toast.success(t('updateSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    },
  });
};

export const useDeleteFloor = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => floorsApi.deleteFloor(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: floorKeys.all });
      queryClient.invalidateQueries({ queryKey: buildingKeys.all });
      toast.success(t('deleteSuccess'));
    },
    onError: (error: any) => {
      if (isArchiveBlockedError(error)) {
        return;
      }
      toast.error(extractUserFriendlyError(error, t('loadError')));
    },
  });
};
