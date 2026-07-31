export enum BuildingType {
  Residential = 'Residential',
  Commercial = 'Commercial',
  MixedUse = 'MixedUse',
}

export enum Governorate {
  Amman = 'Amman',
  Zarqa = 'Zarqa',
  Irbid = 'Irbid',
  Balqa = 'Balqa',
  Madaba = 'Madaba',
  Karak = 'Karak',
  Tafilah = 'Tafilah',
  Maan = 'Maan',
  Aqaba = 'Aqaba',
  Ajloun = 'Ajloun',
  Jerash = 'Jerash',
  Mafraq = 'Mafraq',
}

export interface BuildingAddressDto {
  buildingId: string;
  governorate: Governorate;
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
  buildingType: BuildingType;
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

export interface CreateBuildingRequest {
  name: string;
  internalCode?: string;
  buildingType: BuildingType;
  totalFloors: number;
  constructionYear?: number;
  gpsLatitude?: number;
  gpsLongitude?: number;
  address: {
    governorate: Governorate;
    district: string;
    area?: string;
    streetName?: string;
    postalCode?: string;
  };
}

export interface UpdateBuildingRequest {
  name: string;
  internalCode?: string;
  buildingType: BuildingType;
  totalFloors: number;
  constructionYear?: number;
  gpsLatitude?: number;
  gpsLongitude?: number;
  address: {
    governorate: Governorate;
    district: string;
    area?: string;
    streetName?: string;
    postalCode?: string;
  };
}
