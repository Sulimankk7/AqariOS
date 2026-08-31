import test from 'node:test';
import assert from 'node:assert/strict';
import { buildingSchema } from './buildings.schema';
import { BuildingType, Governorate } from '../constants/buildingEnums';

const validBuilding = {
  name: 'Test Building',
  internalCode: '',
  buildingType: BuildingType.Residential,
  totalFloors: 1,
  address: {
    governorate: Governorate.Amman,
    district: 'Amman',
    area: 'Abdoun',
    streetName: '',
    postalCode: '',
  },
};

test('internal building code remains optional because the backend accepts null', () => {
  assert.equal(buildingSchema.safeParse(validBuilding).success, true);
});

test('city and area must be supplied instead of receiving fabricated mapper fallbacks', () => {
  assert.equal(buildingSchema.safeParse({ ...validBuilding, address: { ...validBuilding.address, district: '' } }).success, false);
  assert.equal(buildingSchema.safeParse({ ...validBuilding, address: { ...validBuilding.address, area: '' } }).success, false);
});
