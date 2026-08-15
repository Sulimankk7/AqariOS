import { http } from '@/shared/lib/http';
import {
  TenantDto,
  TenantDetailDto,
  TenantFamilyMemberDto,
  TenantEmergencyContactDto,
  TenantVehicleDto,
  CreateTenantRequest,
  UpdateTenantRequest,
  CreateTenantFamilyMemberRequest,
  UpdateTenantFamilyMemberRequest,
  CreateTenantEmergencyContactRequest,
  UpdateTenantEmergencyContactRequest,
  CreateTenantVehicleRequest,
  UpdateTenantVehicleRequest,
  ProvisionTenantAccountRequest,
  ProvisionTenantAccountResponseDto,
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

  getFamilyMembers: (tenantId: string): Promise<TenantFamilyMemberDto[]> => {
    return http.get<TenantFamilyMemberDto[]>(`${BASE_PATH}/${tenantId}/family-members`);
  },

  getFamilyMemberById: (tenantId: string, familyMemberId: string): Promise<TenantFamilyMemberDto> => {
    return http.get<TenantFamilyMemberDto>(`${BASE_PATH}/${tenantId}/family-members/${familyMemberId}`);
  },

  createFamilyMember: (tenantId: string, data: CreateTenantFamilyMemberRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${tenantId}/family-members`, data);
  },

  updateFamilyMember: (
    tenantId: string,
    familyMemberId: string,
    data: UpdateTenantFamilyMemberRequest
  ): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${tenantId}/family-members/${familyMemberId}`, data);
  },

  deleteFamilyMember: (tenantId: string, familyMemberId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${tenantId}/family-members/${familyMemberId}`);
  },

  getEmergencyContacts: (tenantId: string): Promise<TenantEmergencyContactDto[]> => {
    return http.get<TenantEmergencyContactDto[]>(`${BASE_PATH}/${tenantId}/emergency-contacts`);
  },

  getEmergencyContactById: (tenantId: string, contactId: string): Promise<TenantEmergencyContactDto> => {
    return http.get<TenantEmergencyContactDto>(`${BASE_PATH}/${tenantId}/emergency-contacts/${contactId}`);
  },

  createEmergencyContact: (tenantId: string, data: CreateTenantEmergencyContactRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${tenantId}/emergency-contacts`, data);
  },

  updateEmergencyContact: (
    tenantId: string,
    contactId: string,
    data: UpdateTenantEmergencyContactRequest
  ): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${tenantId}/emergency-contacts/${contactId}`, data);
  },

  deleteEmergencyContact: (tenantId: string, contactId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${tenantId}/emergency-contacts/${contactId}`);
  },

  getVehicles: (tenantId: string): Promise<TenantVehicleDto[]> => {
    return http.get<TenantVehicleDto[]>(`${BASE_PATH}/${tenantId}/vehicles`);
  },

  getVehicleById: (tenantId: string, vehicleId: string): Promise<TenantVehicleDto> => {
    return http.get<TenantVehicleDto>(`${BASE_PATH}/${tenantId}/vehicles/${vehicleId}`);
  },

  createVehicle: (tenantId: string, data: CreateTenantVehicleRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${tenantId}/vehicles`, data);
  },

  updateVehicle: (
    tenantId: string,
    vehicleId: string,
    data: UpdateTenantVehicleRequest
  ): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${tenantId}/vehicles/${vehicleId}`, data);
  },

  deleteVehicle: (tenantId: string, vehicleId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${tenantId}/vehicles/${vehicleId}`);
  },

  provisionAccount: (
    tenantId: string,
    data?: ProvisionTenantAccountRequest
  ): Promise<ProvisionTenantAccountResponseDto> => {
    return http.post<ProvisionTenantAccountResponseDto>(`${BASE_PATH}/${tenantId}/account`, data || {});
  },
};
