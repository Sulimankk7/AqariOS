import test from 'node:test';
import assert from 'node:assert/strict';
import { Governorate } from '../constants/buildingEnums';
import type { ReverseGeocodingResult } from '../api/locations.api';
import {
  applySuggestedAddress,
  hasApplicableSuggestion,
  isCurrentGeocodingResponse,
  mapJordanGovernorate,
  meaningfulFormattedAddress,
  mergeAutomaticEnrichment,
  toAddressEnrichment,
} from './reverseGeocoding';

const result = (values: Partial<ReverseGeocodingResult> = {}): ReverseGeocodingResult => ({
  language: 'ar',
  ...values,
});

const emptyAddress = {
  governorate: Governorate.Amman,
  district: '',
  area: '',
  streetName: '',
  postalCode: '',
};

test('maps only known Jordan governorate names', () => {
  assert.equal(mapJordanGovernorate('Amman Governorate'), Governorate.Amman);
  assert.equal(mapJordanGovernorate('عمّان'), Governorate.Amman);
  assert.equal(mapJordanGovernorate('Unknown administrative area'), undefined);
  assert.equal(mapJordanGovernorate(null), undefined);
});

test('maps every supported structured field without parsing formatted address', () => {
  const enrichment = toAddressEnrichment(result({
    countryCode: 'JO', governorate: 'إربد', city: 'إربد', district: 'لواء قصبة إربد',
    neighborhood: 'الحي الشرقي', street: 'شارع الجامعة', postalCode: '21110',
    formattedAddress: 'نص لا يجب تحليله',
  }));

  assert.deepEqual(enrichment, {
    governorate: Governorate.Irbid,
    district: 'إربد',
    area: 'الحي الشرقي',
    streetName: 'شارع الجامعة',
    postalCode: '21110',
  });
});

test('partial governorate result does not invent city, area, or street from formatted address', () => {
  const providerResult = result({
    countryCode: 'JO', governorate: 'إربد',
    formattedAddress: "Shari' Sa'd Bin 'Ubadah, Irbid, Jordan",
  });

  assert.deepEqual(toAddressEnrichment(providerResult), { governorate: Governorate.Irbid });
  assert.equal(meaningfulFormattedAddress(providerResult), "Shari' Sa'd Bin 'Ubadah, Irbid, Jordan");
});

test('structured street and city are safe enrichment values', () => {
  assert.deepEqual(toAddressEnrichment(result({ city: 'Irbid', street: "Shari' Sa'd Bin 'Ubadah" })), {
    district: 'Irbid',
    streetName: "Shari' Sa'd Bin 'Ubadah",
  });
});

test('automatic enrichment never overwrites manually edited street or city', () => {
  const current = { ...emptyAddress, district: 'مدينة يدوية', streetName: 'شارع الجامعة' };
  const enrichment = toAddressEnrichment(result({ city: 'Irbid', street: 'Provider Street', neighborhood: 'Provider Area' }));
  const patch = mergeAutomaticEnrichment(current, enrichment, new Set(['district', 'streetName']));

  assert.deepEqual(patch, { area: 'Provider Area' });
});

test('unknown or non-Jordan governorate is never mapped', () => {
  assert.equal(toAddressEnrichment(result({ countryCode: 'JO', governorate: 'Unknown' })).governorate, undefined);
  assert.equal(toAddressEnrichment(result({ countryCode: 'SA', governorate: 'إربد' })).governorate, undefined);
});

test('suggestion action fills only safely applicable empty fields', () => {
  const current = { ...emptyAddress, district: 'إربد', streetName: 'شارع يدوي' };
  const enrichment = toAddressEnrichment(result({ city: 'Irbid', neighborhood: 'Area', street: 'Provider Street', postalCode: '21110' }));
  const protectedFields = new Set<'district' | 'streetName'>(['district', 'streetName']);

  assert.equal(hasApplicableSuggestion(current, enrichment, protectedFields), true);
  assert.deepEqual(applySuggestedAddress(current, enrichment, protectedFields), {
    area: 'Area',
    postalCode: '21110',
  });
});

test('formatted address remains informational when no structured field can be applied', () => {
  const providerResult = result({ formattedAddress: 'Irbid, Jordan' });
  const enrichment = toAddressEnrichment(providerResult);

  assert.equal(meaningfulFormattedAddress(providerResult), 'Irbid, Jordan');
  assert.equal(hasApplicableSuggestion(emptyAddress, enrichment, new Set()), false);
});

test('empty provider result leaves the editable address unchanged and offers no suggestion', () => {
  const providerResult = result();
  const enrichment = toAddressEnrichment(providerResult);

  assert.deepEqual(mergeAutomaticEnrichment(emptyAddress, enrichment, new Set()), {});
  assert.equal(meaningfulFormattedAddress(providerResult), undefined);
  assert.equal(hasApplicableSuggestion(emptyAddress, enrichment, new Set()), false);
});

test('only the latest non-aborted reverse-geocoding response is current', () => {
  assert.equal(isCurrentGeocodingResponse(2, 2, false), true);
  assert.equal(isCurrentGeocodingResponse(1, 2, false), false);
  assert.equal(isCurrentGeocodingResponse(2, 2, true), false);
});
