import test from 'node:test';
import assert from 'node:assert/strict';
import { getApartmentRentDefault } from './leaseDefaults';
import type { ApartmentDto } from '@/features/apartments/types/apartments.types';
import { OccupancyStatus, OwnershipStatus } from '@/features/apartments/types/apartments.types';

const apartment = (overrides: Partial<ApartmentDto> = {}): ApartmentDto => ({
  id: '00000000-0000-0000-0000-000000000001',
  companyId: '00000000-0000-0000-0000-000000000002',
  buildingId: '00000000-0000-0000-0000-000000000003',
  floorId: '00000000-0000-0000-0000-000000000004',
  unitNumber: '101',
  ownershipStatus: OwnershipStatus.CompanyOwned,
  occupancyStatus: OccupancyStatus.Vacant,
  areaSqm: 90,
  bedrooms: 2,
  bathrooms: 2,
  baseRentAmount: 350,
  baseRentCurrency: 'JOD',
  isActive: true,
  createdAt: '2026-09-01T00:00:00Z',
  updatedAt: '2026-09-01T00:00:00Z',
  ...overrides,
});

test('JOD apartment base rent is available as an editable lease default', () => {
  assert.equal(getApartmentRentDefault(apartment()), 350);
});

test('missing, zero, or differently denominated apartment rent is not copied into a JOD lease', () => {
  assert.equal(getApartmentRentDefault(apartment({ baseRentAmount: null })), undefined);
  assert.equal(getApartmentRentDefault(apartment({ baseRentAmount: 0 })), undefined);
  assert.equal(getApartmentRentDefault(apartment({ baseRentCurrency: 'USD' })), undefined);
});
