import { 
  BuildingDto, 
  CreateBuildingRequest, 
  UpdateBuildingRequest 
} from '../types/buildings.types';
import { BuildingType, Governorate } from '../constants/buildingEnums';
import { BuildingFormValues } from '../schemas/buildings.schema';

/**
 * Maps frontend UI form values to authoritative backend POST /api/v1/buildings request payload.
 * Passes through strongly-typed numeric enum values directly without runtime implicit coercion.
 */
export function toCreateBuildingRequest(formValues: BuildingFormValues): CreateBuildingRequest {
  return {
    name: formValues.name,
    buildingType: formValues.buildingType,
    internalCode: formValues.internalCode || undefined,
    constructionYear: formValues.constructionYear,
    gpsLatitude: formValues.gpsLatitude,
    gpsLongitude: formValues.gpsLongitude,
    totalFloors: formValues.totalFloors,
    addressGovernorate: formValues.address.governorate,
    addressCity: formValues.address.district || 'Amman',
    addressNeighborhood: formValues.address.area || formValues.address.district || 'General',
    addressStreet: formValues.address.streetName || undefined,
    addressPostalCode: formValues.address.postalCode || undefined,
  };
}

/**
 * Maps frontend UI form values to authoritative backend PUT /api/v1/buildings/{id} request payload.
 * Passes through strongly-typed numeric enum values directly without runtime implicit coercion.
 */
export function toUpdateBuildingRequest(formValues: BuildingFormValues): UpdateBuildingRequest {
  return {
    name: formValues.name,
    buildingType: formValues.buildingType,
    internalCode: formValues.internalCode || undefined,
    constructionYear: formValues.constructionYear,
    gpsLatitude: formValues.gpsLatitude,
    gpsLongitude: formValues.gpsLongitude,
    totalFloors: formValues.totalFloors,
    addressGovernorate: formValues.address.governorate,
    addressCity: formValues.address.district || 'Amman',
    addressNeighborhood: formValues.address.area || formValues.address.district || 'General',
    addressStreet: formValues.address.streetName || undefined,
    addressPostalCode: formValues.address.postalCode || undefined,
  };
}

/**
 * Maps backend BuildingDto to frontend UI form default values when prefilling edit forms.
 * Strongly types enum properties as BuildingType / Governorate numeric values.
 */
export function toBuildingForm(building: BuildingDto): BuildingFormValues {
  return {
    name: building.name || '',
    internalCode: building.internalCode || '',
    buildingType: (building.buildingType as BuildingType) ?? BuildingType.Residential,
    totalFloors: building.totalFloors || 1,
    constructionYear: building.constructionYear || undefined,
    gpsLatitude: building.gpsLatitude || undefined,
    gpsLongitude: building.gpsLongitude || undefined,
    address: {
      governorate: (building.address?.governorate as Governorate) ?? Governorate.Amman,
      district: building.address?.district || '',
      area: building.address?.area || '',
      streetName: building.address?.streetName || '',
      postalCode: building.address?.postalCode || '',
    },
  };
}
