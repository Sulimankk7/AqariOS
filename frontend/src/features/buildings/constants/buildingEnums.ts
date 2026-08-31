/**
 * ARCHITECTURAL NOTICE:
 * This module is the single frontend mirror of the authoritative backend enums:
 * - BuildingType: PropertyOS.Domain.Properties.Enums.BuildingType (src/PropertyOS.Domain/Properties/Enums/BuildingType.cs)
 * - Governorate: PropertyOS.Domain.Properties.Enums.Governorate (src/PropertyOS.Domain/Properties/Enums/Governorate.cs)
 *
 * DO NOT alter numeric enum values without a corresponding backend C# migration.
 * All frontend components, forms, tables, details pages, filters, and mappers MUST consume
 * these shared definitions to prevent runtime enum divergence.
 */

export enum BuildingType {
  Residential = 0,
  Commercial = 1,
  MixedUse = 2,
}

export enum Governorate {
  Amman = 0,
  Zarqa = 1,
  Irbid = 2,
  Balqa = 3,
  Madaba = 4,
  Karak = 5,
  Tafilah = 6,
  Maan = 7,
  Aqaba = 8,
  Ajloun = 9,
  Jerash = 10,
  Mafraq = 11,
}

export interface EnumOption<T extends number = number> {
  value: T;
  labelKey: string;
}

export const BUILDING_TYPE_OPTIONS: EnumOption<BuildingType>[] = [
  { value: BuildingType.Residential, labelKey: 'buildingTypes.residential' },
  { value: BuildingType.Commercial, labelKey: 'buildingTypes.commercial' },
  { value: BuildingType.MixedUse, labelKey: 'buildingTypes.mixedUse' },
];

export const GOVERNORATE_OPTIONS: EnumOption<Governorate>[] = [
  { value: Governorate.Amman, labelKey: 'governorates.amman' },
  { value: Governorate.Zarqa, labelKey: 'governorates.zarqa' },
  { value: Governorate.Irbid, labelKey: 'governorates.irbid' },
  { value: Governorate.Balqa, labelKey: 'governorates.balqa' },
  { value: Governorate.Madaba, labelKey: 'governorates.madaba' },
  { value: Governorate.Karak, labelKey: 'governorates.karak' },
  { value: Governorate.Tafilah, labelKey: 'governorates.tafilah' },
  { value: Governorate.Maan, labelKey: 'governorates.maan' },
  { value: Governorate.Aqaba, labelKey: 'governorates.aqaba' },
  { value: Governorate.Ajloun, labelKey: 'governorates.ajloun' },
  { value: Governorate.Jerash, labelKey: 'governorates.jerash' },
  { value: Governorate.Mafraq, labelKey: 'governorates.mafraq' },
];

export function buildingTypeToLabel(
  value: BuildingType | number | undefined | null, 
  t?: (key: string) => string
): string {
  if (value === undefined || value === null) return '-';
  const option = BUILDING_TYPE_OPTIONS.find((opt) => opt.value === value);
  if (!option) return t ? t('unknown') : '-';
  return t ? t(option.labelKey) : option.labelKey;
}

export function governorateToLabel(
  value: Governorate | number | undefined | null, 
  t?: (key: string) => string
): string {
  if (value === undefined || value === null) return '-';
  const option = GOVERNORATE_OPTIONS.find((opt) => opt.value === value);
  if (!option) return t ? t('unknown') : '-';
  return t ? t(option.labelKey) : option.labelKey;
}
