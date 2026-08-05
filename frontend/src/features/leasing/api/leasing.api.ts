import { http } from '@/shared/lib/http';
import {
  LeaseContractDto,
  LeaseContractDetailDto,
  CreateLeaseContractRequest,
  UpdateDraftLeaseContractRequest,
  RenewLeaseContractRequest,
  TerminateLeaseContractRequest,
  AttachContractDocumentRequest,
  TenantLookupDto,
  ContractDocumentDownloadUrlDto,
} from '../types/leasing.types';

const BASE_PATH = '/api/v1/leasing/contracts';
const TENANTS_PATH = '/api/v1/leasing/tenants';

export const leasingApi = {
  searchContracts: (searchTerm: string = '', pageSize: number = 50): Promise<LeaseContractDto[]> => {
    const params = new URLSearchParams();
    if (searchTerm) params.append('searchTerm', searchTerm);
    if (pageSize) params.append('pageSize', pageSize.toString());
    const query = params.toString() ? `?${params.toString()}` : '';
    return http.get<LeaseContractDto[]>(`${BASE_PATH}/search${query}`);
  },

  getExpiringContracts: (daysAhead: number = 30): Promise<LeaseContractDto[]> => {
    return http.get<LeaseContractDto[]>(`${BASE_PATH}/expiring?daysAhead=${daysAhead}`);
  },

  getApartmentLeaseHistory: (apartmentId: string, pageSize: number = 50): Promise<LeaseContractDto[]> => {
    return http.get<LeaseContractDto[]>(`${BASE_PATH}/history/apartment/${apartmentId}?pageSize=${pageSize}`);
  },

  getContractById: (id: string): Promise<LeaseContractDetailDto> => {
    return http.get<LeaseContractDetailDto>(`${BASE_PATH}/${id}`);
  },

  createContract: (data: CreateLeaseContractRequest): Promise<string> => {
    return http.post<string>(BASE_PATH, data);
  },

  updateDraftContract: (id: string, data: UpdateDraftLeaseContractRequest): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${id}`, data);
  },

  activateContract: (id: string): Promise<void> => {
    return http.post<void>(`${BASE_PATH}/${id}/activate`);
  },

  terminateContract: (id: string, data: TerminateLeaseContractRequest): Promise<void> => {
    return http.post<void>(`${BASE_PATH}/${id}/terminate`, data);
  },

  renewContract: (id: string, data: RenewLeaseContractRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${id}/renew`, data);
  },

  attachDocument: (id: string, data: AttachContractDocumentRequest): Promise<string> => {
    return http.post<string>(`${BASE_PATH}/${id}/documents`, data);
  },

  getDownloadUrl: (contractId: string, documentId: string, inline: boolean = false): Promise<ContractDocumentDownloadUrlDto> => {
    return http.get<ContractDocumentDownloadUrlDto>(`${BASE_PATH}/${contractId}/documents/${documentId}/download?inline=${inline}&redirect=false`);
  },

  replaceDocument: (contractId: string, documentId: string, data: { newFileId: string; description?: string | null }): Promise<void> => {
    return http.put<void>(`${BASE_PATH}/${contractId}/documents/${documentId}/replace`, data);
  },

  deleteDocument: (contractId: string, documentId: string): Promise<void> => {
    return http.delete<void>(`${BASE_PATH}/${contractId}/documents/${documentId}`);
  },

  getTenants: (searchTerm: string = ''): Promise<TenantLookupDto[]> => {
    const query = searchTerm ? `?searchTerm=${encodeURIComponent(searchTerm)}` : '';
    return http.get<TenantLookupDto[]>(`${TENANTS_PATH}${query}`);
  },
};

