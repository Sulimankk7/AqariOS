import { http } from '@/shared/lib/http';
import { FloorDto, CreateFloorRequest, UpdateFloorRequest } from '../types/floors.types';
import { FloorFormValues } from '../schemas/floors.schema';

export const floorsApi = {
  getNextFloorNumber(buildingId: string): Promise<{ value: string }> {
    return http.get(`/api/v1/buildings/${buildingId}/floors/next-number`);
  },
  /**
   * Lists all floors belonging to a specified building.
   * GET /api/v1/buildings/{buildingId}/floors
   */
  getFloorsByBuilding(buildingId: string): Promise<FloorDto[]> {
    return http.get<FloorDto[]>(`/api/v1/buildings/${buildingId}/floors`);
  },

  /**
   * Retrieves a floor by its unique identifier.
   * GET /api/v1/floors/{id}
   */
  getFloorById(id: string): Promise<FloorDto> {
    return http.get<FloorDto>(`/api/v1/floors/${id}`);
  },

  /**
   * Creates a new floor inside a building.
   * POST /api/v1/buildings/{buildingId}/floors
   */
  createFloor(buildingId: string, data: FloorFormValues | CreateFloorRequest): Promise<void> {
    const payload: CreateFloorRequest = {
      floorNumber: data.floorNumber,
      floorLabel: data.floorLabel,
      floorType: data.floorType,
    };
    return http.post<void>(`/api/v1/buildings/${buildingId}/floors`, payload);
  },

  /**
   * Updates a floor's label or type.
   * PUT /api/v1/floors/{id}
   */
  updateFloor(id: string, data: FloorFormValues | UpdateFloorRequest): Promise<void> {
    const payload: UpdateFloorRequest = {
      floorLabel: data.floorLabel,
      floorType: data.floorType,
    };
    return http.put<void>(`/api/v1/floors/${id}`, payload);
  },

  /**
   * Soft-deletes/archives a floor.
   * DELETE /api/v1/floors/{id}
   */
  deleteFloor(id: string): Promise<void> {
    return http.delete<void>(`/api/v1/floors/${id}`);
  },
};
