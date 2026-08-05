import { http } from '@/shared/lib/http';
import { 
  ApartmentDto, 
  CreateApartmentRequest, 
  UpdateApartmentRequest, 
  ListApartmentsParams 
} from '../types/apartments.types';
import { ApartmentFormValues } from '../schemas/apartments.schema';
import { toCreateApartmentRequest, toUpdateApartmentRequest } from '../utils/apartmentMappers';

export const apartmentsApi = {
  /**
   * Lists apartments across floors, optionally filtered by buildingId or floorId.
   * GET /api/v1/apartments
   */
  getApartments(params?: ListApartmentsParams): Promise<ApartmentDto[]> {
    const queryParams = new URLSearchParams();
    if (params?.buildingId) queryParams.append('buildingId', params.buildingId);
    if (params?.floorId) queryParams.append('floorId', params.floorId);
    const queryString = queryParams.toString();
    const url = queryString ? `/api/v1/apartments?${queryString}` : '/api/v1/apartments';
    return http.get<ApartmentDto[]>(url);
  },

  /**
   * Retrieves an apartment by its unique identifier.
   * GET /api/v1/apartments/{id}
   */
  getApartmentById(id: string): Promise<ApartmentDto> {
    return http.get<ApartmentDto>(`/api/v1/apartments/${id}`);
  },

  /**
   * Creates a new apartment unit under a specific floor.
   * POST /api/v1/floors/{floorId}/apartments
   */
  createApartment(floorId: string, data: ApartmentFormValues | CreateApartmentRequest): Promise<void> {
    const payload = 'unitNumber' in data && !('floorId' in data)
      ? data as CreateApartmentRequest
      : toCreateApartmentRequest(data as ApartmentFormValues);
      
    return http.post<void>(`/api/v1/floors/${floorId}/apartments`, payload);
  },

  /**
   * Updates an apartment's base rent amount and currency terms.
   * PUT /api/v1/apartments/{id}
   */
  updateApartment(id: string, data: ApartmentFormValues | UpdateApartmentRequest): Promise<void> {
    const payload = 'baseRentCurrency' in data && !('floorId' in data)
      ? data as UpdateApartmentRequest
      : toUpdateApartmentRequest(data as ApartmentFormValues);

    return http.put<void>(`/api/v1/apartments/${id}`, payload);
  },

  /**
   * Soft-deletes/archives an apartment unit.
   * DELETE /api/v1/apartments/{id}
   */
  deleteApartment(id: string): Promise<void> {
    return http.delete<void>(`/api/v1/apartments/${id}`);
  },
};
