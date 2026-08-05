import { OwnershipStatus, OccupancyStatus } from '../constants/apartmentEnums';

export { OwnershipStatus, OccupancyStatus };

/**
 * DTO returned by GET /api/v1/apartments and GET /api/v1/apartments/{id}.
 * Matches PropertyOS.Application.Properties.Apartments.Queries.Common.ApartmentDto exactly.
 */
export interface ApartmentDto {
  id: string;
  companyId: string;
  buildingId: string;
  floorId: string;
  unitNumber: string;
  ownershipStatus: OwnershipStatus;
  externalOwnerName?: string | null;
  externalOwnerPhone?: string | null;
  occupancyStatus: OccupancyStatus;
  areaSqm: number;
  bedrooms: number;
  bathrooms: number;
  baseRentAmount?: number | null;
  baseRentCurrency: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * Payload sent to POST /api/v1/floors/{floorId}/apartments.
 * Matches PropertyOS.Api.Models.Properties.CreateApartmentRequest exactly.
 */
export interface CreateApartmentRequest {
  unitNumber: string;
  areaSqm: number;
  ownershipStatus: OwnershipStatus;
  externalOwnerName?: string;
  externalOwnerPhone?: string;
  bedrooms: number;
  bathrooms: number;
  baseRentAmount?: number;
  baseRentCurrency: string;
}

/**
 * Payload sent to PUT /api/v1/apartments/{id}.
 * Matches PropertyOS.Api.Models.Properties.UpdateApartmentRequest exactly.
 */
export interface UpdateApartmentRequest {
  baseRentAmount?: number;
  baseRentCurrency: string;
}

export interface ListApartmentsParams {
  buildingId?: string;
  floorId?: string;
}
