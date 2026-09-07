import { Governorate } from '../constants/buildingEnums';
import type { ReverseGeocodingResult } from '../api/locations.api';

export type AddressField = 'governorate' | 'district' | 'area' | 'streetName' | 'postalCode';

export interface AddressEnrichment {
  governorate?: Governorate;
  district?: string;
  area?: string;
  streetName?: string;
  postalCode?: string;
}

export interface AddressValues {
  governorate: Governorate;
  district: string;
  area: string;
  streetName?: string;
  postalCode?: string;
}

const names: Record<string, Governorate> = {
  amman: Governorate.Amman, 'عمان': Governorate.Amman, 'عمّان': Governorate.Amman,
  zarqa: Governorate.Zarqa, 'الزرقاء': Governorate.Zarqa,
  irbid: Governorate.Irbid, 'إربد': Governorate.Irbid, 'اربد': Governorate.Irbid,
  balqa: Governorate.Balqa, 'البلقاء': Governorate.Balqa,
  madaba: Governorate.Madaba, 'مأدبا': Governorate.Madaba, 'مادبا': Governorate.Madaba,
  karak: Governorate.Karak, 'الكرك': Governorate.Karak,
  tafilah: Governorate.Tafilah, 'الطفيلة': Governorate.Tafilah,
  "ma'an": Governorate.Maan, maan: Governorate.Maan, 'معان': Governorate.Maan,
  aqaba: Governorate.Aqaba, 'العقبة': Governorate.Aqaba,
  ajloun: Governorate.Ajloun, 'عجلون': Governorate.Ajloun,
  jerash: Governorate.Jerash, 'جرش': Governorate.Jerash,
  mafraq: Governorate.Mafraq, 'المفرق': Governorate.Mafraq,
};

export function mapJordanGovernorate(value?: string | null): Governorate | undefined {
  if (!value) return undefined;
  const normalized = value.trim().toLocaleLowerCase().replace(/ governorate$/i, '').trim();
  return names[normalized];
}

function meaningful(value?: string | null): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

/** Maps provider-structured data only. FormattedAddress is deliberately never parsed. */
export function toAddressEnrichment(result: ReverseGeocodingResult): AddressEnrichment {
  const governorate = result.countryCode?.toUpperCase() === 'JO'
    ? mapJordanGovernorate(result.governorate)
    : undefined;

  const enrichment: AddressEnrichment = {
    governorate,
    district: meaningful(result.city) ?? meaningful(result.district),
    area: meaningful(result.neighborhood),
    streetName: meaningful(result.street),
    postalCode: meaningful(result.postalCode),
  };

  return Object.fromEntries(
    Object.entries(enrichment).filter(([, value]) => value !== undefined),
  ) as AddressEnrichment;
}

export function meaningfulFormattedAddress(result: ReverseGeocodingResult): string | undefined {
  return meaningful(result.formattedAddress);
}

export function mergeAutomaticEnrichment(
  current: AddressValues,
  enrichment: AddressEnrichment,
  manuallyEdited: ReadonlySet<AddressField>,
): Partial<AddressValues> {
  return Object.fromEntries(
    (Object.keys(enrichment) as AddressField[])
      .filter((field) => enrichment[field] !== undefined && !manuallyEdited.has(field))
      .map((field) => [field, enrichment[field]]),
  ) as Partial<AddressValues>;
}

/** The explicit suggestion action may fill empty, non-manual fields only. */
export function applySuggestedAddress(
  current: AddressValues,
  enrichment: AddressEnrichment,
  manuallyEdited: ReadonlySet<AddressField>,
): Partial<AddressValues> {
  return Object.fromEntries(
    (Object.keys(enrichment) as AddressField[])
      .filter((field) => {
        if (enrichment[field] === undefined || manuallyEdited.has(field)) return false;
        const value = current[field];
        return typeof value === 'string' && value.trim().length === 0;
      })
      .map((field) => [field, enrichment[field]]),
  ) as Partial<AddressValues>;
}

export function hasApplicableSuggestion(
  current: AddressValues,
  enrichment: AddressEnrichment,
  manuallyEdited: ReadonlySet<AddressField>,
): boolean {
  return Object.keys(applySuggestedAddress(current, enrichment, manuallyEdited)).length > 0;
}

export function isCurrentGeocodingResponse(
  requestSequence: number,
  latestSequence: number,
  aborted: boolean,
): boolean {
  return !aborted && requestSequence === latestSequence;
}
