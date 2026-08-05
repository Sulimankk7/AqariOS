import { http } from '@/shared/lib/http';
import { 
  BuildingDto, 
  CreateBuildingRequest, 
  UpdateBuildingRequest 
} from '../types/buildings.types';
import { BuildingFormValues } from '../schemas/buildings.schema';
import { toCreateBuildingRequest, toUpdateBuildingRequest } from '../utils/buildingMappers';

const BASE_PATH = '/api/v1/buildings';

export const buildingsApi = {
  getBuildings: (): Promise<BuildingDto[]> => {
    return http.get<BuildingDto[]>(BASE_PATH);
  },

  getBuildingById: (id: string): Promise<BuildingDto> => {
    return http.get<BuildingDto>(`${BASE_PATH}/${id}`);
  },

  createBuilding: (data: BuildingFormValues | CreateBuildingRequest): Promise<void> => {
    const payload = 'address' in data && typeof data.address === 'object'
      ? toCreateBuildingRequest(data as BuildingFormValues)
      : (data as CreateBuildingRequest);
    return http.post<void>(BASE_PATH, payload);
  },

  updateBuilding: (id: string, data: BuildingFormValues | UpdateBuildingRequest): Promise<void> => {
    const payload = 'address' in data && typeof data.address === 'object'
      ? toUpdateBuildingRequest(data as BuildingFormValues)
      : (data as UpdateBuildingRequest);
    return http.put<void>(`${BASE_PATH}/${id}`, payload);
  },

  deleteBuilding: (id: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${id}`);
  },
};
