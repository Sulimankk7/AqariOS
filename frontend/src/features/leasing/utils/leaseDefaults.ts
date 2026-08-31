import type { ApartmentDto } from '@/features/apartments/types/apartments.types';

export function getApartmentRentDefault(apartment: ApartmentDto | undefined): number | undefined {
  if (!apartment || apartment.baseRentAmount == null || apartment.baseRentAmount <= 0) {
    return undefined;
  }

  // LeaseContract.Currency is currently backend-controlled as JOD. Never copy an
  // apartment amount denominated in another currency into a JOD lease.
  if (apartment.baseRentCurrency !== 'JOD') {
    return undefined;
  }

  return apartment.baseRentAmount;
}
