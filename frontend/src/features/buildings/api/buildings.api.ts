import { http } from '@/shared/lib/http';
import { 
  BuildingDto, 
  CreateBuildingRequest, 
  UpdateBuildingRequest 
} from '../types/buildings.types';

const BASE_PATH = '/api/v1/buildings';

export const buildingsApi = {
  getBuildings: (): Promise<BuildingDto[]> => {
    return http.get<BuildingDto[]>(BASE_PATH);
  },

  getBuildingById: (id: string): Promise<BuildingDto> => {
    return http.get<BuildingDto>(`${BASE_PATH}/${id}`);
  },

  createBuilding: (data: CreateBuildingRequest): Promise<void> => {
    return http.post<void>(BASE_PATH, data);
  },

  updateBuilding: (id: string, data: UpdateBuildingRequest): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${id}`, data);
  },

  deleteBuilding: (id: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${id}`);
  },
};
