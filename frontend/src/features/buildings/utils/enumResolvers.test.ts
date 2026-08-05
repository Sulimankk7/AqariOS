import test from 'node:test';
import assert from 'node:assert';
import { buildingTypeToLabel, BuildingType } from '../constants/buildingEnums';

test('buildingTypeToLabel returns human-readable label instead of numeric int value for enum values', () => {
  // Assert label for numeric 0 (Residential) is returned, not "0"
  assert.strictEqual(buildingTypeToLabel(0), 'buildingTypes.residential');
  assert.notStrictEqual(buildingTypeToLabel(0), '0');

  // Assert label for numeric 1 (Commercial) is returned, not "1"
  assert.strictEqual(buildingTypeToLabel(1), 'buildingTypes.commercial');
  assert.notStrictEqual(buildingTypeToLabel(1), '1');

  // Assert label for BuildingType.MixedUse
  assert.strictEqual(buildingTypeToLabel(BuildingType.MixedUse), 'buildingTypes.mixedUse');

  // Assert localization resolver output
  const mockT = (key: string) => (key === 'buildingTypes.residential' ? 'Residential' : key);
  assert.strictEqual(buildingTypeToLabel(BuildingType.Residential, mockT), 'Residential');
});
