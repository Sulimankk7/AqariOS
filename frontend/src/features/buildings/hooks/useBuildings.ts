import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { buildingsApi } from '../api/buildings.api';
import { buildingKeys } from './buildingKeys';
import { CreateBuildingRequest, UpdateBuildingRequest } from '../types/buildings.types';
import { BuildingFormValues } from '../schemas/buildings.schema';
import { toast } from 'sonner';
import { getBuildingTranslation } from '../constants/translations';
import { isArchiveBlockedError } from '@/shared/lib/archiveBlocked';
import { getRuntimeLanguage } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils/errorHandling';

const t = (key: string) => getBuildingTranslation(key, getRuntimeLanguage());

export const useBuildings = () => {
  return useQuery({
    queryKey: buildingKeys.lists(),
    queryFn: () => buildingsApi.getBuildings(),
  });
};

export const useBuilding = (id: string) => {
  return useQuery({
    queryKey: buildingKeys.detail(id),
    queryFn: () => buildingsApi.getBuildingById(id),
    enabled: !!id,
  });
};

export const useCreateBuilding = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BuildingFormValues | CreateBuildingRequest) => buildingsApi.createBuilding(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: buildingKeys.all });
      toast.success(t('createSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    }
  });
};

export const useUpdateBuilding = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: BuildingFormValues | UpdateBuildingRequest }) => 
      buildingsApi.updateBuilding(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: buildingKeys.all });
      queryClient.invalidateQueries({ queryKey: buildingKeys.detail(variables.id) });
      toast.success(t('updateSuccess'));
    },
    onError: (error: any) => {
      toast.error(extractUserFriendlyError(error, t('loadError')));
    }
  });
};

export const useDeleteBuilding = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => buildingsApi.deleteBuilding(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: buildingKeys.all });
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
