import { LeaseContractDto } from '@/features/leasing/types/leasing.types';

export interface TenantDto {
  id: string;
  companyId: string;
  name: string;
  nationalId: string;
  phone: string;
  email?: string | null;
  occupation?: string | null;
  employer?: string | null;
  userId?: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy?: string | null;
  updatedBy?: string | null;
}

export interface TenantFamilyMemberDto {
  id: string;
  name: string;
  relationshipType: string;
  ageBracket?: string | null;
  createdAt: string;
}

export interface TenantEmergencyContactDto {
  id: string;
  name: string;
  relationshipType: string;
  phone: string;
  createdAt: string;
}

export interface TenantVehicleDto {
  id: string;
  plateNumber: string;
  makeModel: string;
  color: string;
  createdAt: string;
}

export interface TenantDetailDto extends TenantDto {
  familyMembers: TenantFamilyMemberDto[];
  emergencyContacts: TenantEmergencyContactDto[];
  vehicles: TenantVehicleDto[];
}

export interface CreateTenantRequest {
  name: string;
  nationalId: string;
  phone: string;
  email: string;
  occupation?: string | null;
  employer?: string | null;
}

export interface UpdateTenantRequest {
  name: string;
  nationalId: string;
  phone: string;
  email?: string | null;
  occupation?: string | null;
  employer?: string | null;
}

export interface CreateTenantFamilyMemberRequest {
  name: string;
  relationshipType: string;
  ageBracket?: string | null;
}

export interface UpdateTenantFamilyMemberRequest {
  name: string;
  relationshipType: string;
  ageBracket?: string | null;
}

export interface CreateTenantEmergencyContactRequest {
  name: string;
  relationshipType: string;
  phone: string;
}

export interface UpdateTenantEmergencyContactRequest {
  name: string;
  relationshipType: string;
  phone: string;
}

export interface CreateTenantVehicleRequest {
  plateNumber: string;
  makeModel: string;
  color: string;
}

export interface UpdateTenantVehicleRequest {
  plateNumber: string;
  makeModel: string;
  color: string;
}

export enum TenantProvisioningContactMethod {
  Phone = 'Phone',
  Email = 'Email',
}

export interface ProvisionTenantAccountRequest {
  contactMethod?: TenantProvisioningContactMethod;
  phone?: string | null;
  email?: string | null;
}

export interface ProvisionTenantAccountResponseDto {
  tenantId: string;
  userId: string;
  companyId: string;
  activationToken?: string | null;
  expiresAt: string;
  emailSent?: boolean;
  smsSent?: boolean;
}

export { type LeaseContractDto };
