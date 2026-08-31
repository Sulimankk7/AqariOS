/**
 * Single source of truth for Floors module enums.
 * Mirrors C# backend enum: PropertyOS.Domain.Properties.Enums.FloorType
 * Standard = 0, Mezzanine = 1, Basement = 2, Penthouse = 3, Service = 4
 */

export enum FloorType {
  Standard = 0,
  Mezzanine = 1,
  Basement = 2,
  Penthouse = 3,
  Service = 4,
}

export interface FloorTypeOption {
  value: FloorType;
  labelKey: string;
}

export const FLOOR_TYPE_OPTIONS: FloorTypeOption[] = [
  { value: FloorType.Standard, labelKey: 'floorTypes.standard' },
  { value: FloorType.Mezzanine, labelKey: 'floorTypes.mezzanine' },
  { value: FloorType.Basement, labelKey: 'floorTypes.basement' },
  { value: FloorType.Penthouse, labelKey: 'floorTypes.penthouse' },
  { value: FloorType.Service, labelKey: 'floorTypes.service' },
];

export function floorTypeToLabel(
  value: FloorType | number | undefined | null,
  t?: (key: string) => string
): string {
  if (value === undefined || value === null) return '-';
  const option = FLOOR_TYPE_OPTIONS.find((opt) => opt.value === value);
  if (!option) return t ? t('unknown') : '-';
  return t ? t(option.labelKey) : option.labelKey;
}
