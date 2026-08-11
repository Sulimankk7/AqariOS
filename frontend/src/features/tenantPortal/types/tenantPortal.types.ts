/**
 * Tenant Portal Types — Synchronized 1:1 with ASP.NET Core Backend DTOs.
 * Source DTO: PropertyOS.Application.Leasing.Queries.GetTenantById.TenantDetailDto
 */

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

export interface TenantDetailDto {
  id: string;
  companyId: string;
  name: string;
  nationalId: string;
  phone: string;
  occupation?: string | null;
  employer?: string | null;
  userId?: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy?: string | null;
  updatedBy?: string | null;
  familyMembers: TenantFamilyMemberDto[];
  emergencyContacts: TenantEmergencyContactDto[];
  vehicles: TenantVehicleDto[];
}
