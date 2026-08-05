import { http } from '@/shared/lib/http';
import {
  TenantDto,
  TenantDetailDto,
  CreateTenantRequest,
  UpdateTenantRequest,
  LeaseContractDto,
} from '../types/tenants.types';

const BASE_PATH = '/api/v1/leasing/tenants';

export const tenantsApi = {
  searchTenants: (searchTerm: string = ''): Promise<TenantDto[]> => {
    const query = searchTerm ? `?searchTerm=${encodeURIComponent(searchTerm)}` : '';
    return http.get<TenantDto[]>(`${BASE_PATH}${query}`);
  },

  getTenantById: (tenantId: string): Promise<TenantDetailDto> => {
    return http.get<TenantDetailDto>(`${BASE_PATH}/${tenantId}`);
  },

  createTenant: (data: CreateTenantRequest): Promise<string> => {
    return http.post<string>(BASE_PATH, data);
  },

  updateTenant: (tenantId: string, data: UpdateTenantRequest): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${tenantId}`, data);
  },

  deleteTenant: (tenantId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${tenantId}`);
  },

  getLeaseHistoryByTenant: (tenantId: string, pageSize: number = 50): Promise<LeaseContractDto[]> => {
    return http.get<LeaseContractDto[]>(`${BASE_PATH}/${tenantId}/leases?pageSize=${pageSize}`);
  },
};
