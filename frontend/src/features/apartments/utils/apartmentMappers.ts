import { 
  ApartmentDto, 
  CreateApartmentRequest, 
  UpdateApartmentRequest 
} from '../types/apartments.types';
import { OwnershipStatus } from '../constants/apartmentEnums';
import { ApartmentFormValues } from '../schemas/apartments.schema';

/**
 * Maps frontend UI form values to backend POST /api/v1/floors/{floorId}/apartments request payload.
 * Strictly passes through strongly-typed numeric enum values directly without runtime implicit coercion.
 */
export function toCreateApartmentRequest(formValues: ApartmentFormValues): CreateApartmentRequest {
  return {
    unitNumber: formValues.unitNumber,
    areaSqm: formValues.areaSqm,
    ownershipStatus: formValues.ownershipStatus,
    externalOwnerName: formValues.externalOwnerName || undefined,
    externalOwnerPhone: formValues.externalOwnerPhone || undefined,
    bedrooms: formValues.bedrooms ?? 0,
    bathrooms: formValues.bathrooms ?? 0,
    baseRentAmount: formValues.baseRentAmount !== undefined ? formValues.baseRentAmount : undefined,
    baseRentCurrency: formValues.baseRentCurrency || 'JOD',
  };
}

/**
 * Maps frontend UI form values to backend PUT /api/v1/apartments/{id} request payload.
 * Note: Backend UpdateApartmentRequest only accepts BaseRentAmount and BaseRentCurrency.
 */
export function toUpdateApartmentRequest(formValues: ApartmentFormValues): UpdateApartmentRequest {
  return {
    baseRentAmount: formValues.baseRentAmount !== undefined ? formValues.baseRentAmount : undefined,
    baseRentCurrency: formValues.baseRentCurrency || 'JOD',
  };
}

/**
 * Maps backend ApartmentDto to frontend UI form default values when prefilling edit forms.
 */
export function toApartmentForm(dto: ApartmentDto): ApartmentFormValues {
  return {
    floorId: dto.floorId || '',
    unitNumber: dto.unitNumber || '',
    areaSqm: dto.areaSqm || 0,
    ownershipStatus: (dto.ownershipStatus as OwnershipStatus) ?? OwnershipStatus.CompanyOwned,
    externalOwnerName: dto.externalOwnerName || '',
    externalOwnerPhone: dto.externalOwnerPhone || '',
    bedrooms: dto.bedrooms ?? 0,
    bathrooms: dto.bathrooms ?? 0,
    baseRentAmount: dto.baseRentAmount !== null && dto.baseRentAmount !== undefined ? dto.baseRentAmount : undefined,
    baseRentCurrency: dto.baseRentCurrency || 'JOD',
  };
}
