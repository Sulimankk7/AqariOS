import { BuildingType, Governorate } from '../constants/buildingEnums';

export { BuildingType, Governorate };

export interface BuildingAddressDto {
  buildingId: string;
  governorate: Governorate | number;
  district: string;
  area?: string;
  streetName?: string;
  postalCode?: string;
}

export interface BuildingDto {
  id: string;
  companyId: string;
  name: string;
  internalCode?: string;
  buildingType: BuildingType | number;
  totalFloors: number;
  constructionYear?: number;
  gpsLatitude?: number;
  gpsLongitude?: number;
  totalApartmentsCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  address?: BuildingAddressDto;
}

/**
 * Authoritative backend request contract for POST /api/v1/buildings
 */
export interface CreateBuildingRequest {
  name: string;
  buildingType: number;
  internalCode?: string;
  constructionYear?: number;
  gpsLatitude?: number;
  gpsLongitude?: number;
  totalFloors: number;
  addressGovernorate: number;
  addressCity: string;
  addressNeighborhood: string;
  addressStreet?: string;
  addressPostalCode?: string;
}

/**
 * Authoritative backend request contract for PUT /api/v1/buildings/{id}
 */
export interface UpdateBuildingRequest {
  name: string;
  buildingType: number;
  internalCode?: string;
  constructionYear?: number;
  gpsLatitude?: number;
  gpsLongitude?: number;
  totalFloors: number;
  addressGovernorate: number;
  addressCity: string;
  addressNeighborhood: string;
  addressStreet?: string;
  addressPostalCode?: string;
}
