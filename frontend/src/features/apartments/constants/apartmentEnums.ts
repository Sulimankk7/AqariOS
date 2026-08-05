/**
 * ARCHITECTURAL NOTICE:
 * Single source of truth for Apartments module enums.
 * Mirrors authoritative C# backend enums:
 * - OwnershipStatus: PropertyOS.Domain.Properties.Enums.OwnershipStatus (CompanyOwned = 0, ThirdPartyOwned = 1)
 * - OccupancyStatus: PropertyOS.Domain.Properties.Enums.OccupancyStatus (Vacant = 0, Occupied = 1, UnderMaintenance = 2, Listed = 3)
 */

export enum OwnershipStatus {
  CompanyOwned = 0,
  ThirdPartyOwned = 1,
}

export enum OccupancyStatus {
  Vacant = 0,
  Occupied = 1,
  UnderMaintenance = 2,
  Listed = 3,
}

export interface EnumOption<T extends number = number> {
  value: T;
  labelKey: string;
}

export const OWNERSHIP_STATUS_OPTIONS: EnumOption<OwnershipStatus>[] = [
  { value: OwnershipStatus.CompanyOwned, labelKey: 'ownershipStatus.companyOwned' },
  { value: OwnershipStatus.ThirdPartyOwned, labelKey: 'ownershipStatus.thirdPartyOwned' },
];

export const OCCUPANCY_STATUS_OPTIONS: EnumOption<OccupancyStatus>[] = [
  { value: OccupancyStatus.Vacant, labelKey: 'occupancyStatus.vacant' },
  { value: OccupancyStatus.Occupied, labelKey: 'occupancyStatus.occupied' },
  { value: OccupancyStatus.UnderMaintenance, labelKey: 'occupancyStatus.underMaintenance' },
  { value: OccupancyStatus.Listed, labelKey: 'occupancyStatus.listed' },
];

export function ownershipStatusToLabel(
  value: OwnershipStatus | number | undefined | null,
  t?: (key: string) => string
): string {
  if (value === undefined || value === null) return '-';
  const option = OWNERSHIP_STATUS_OPTIONS.find((opt) => opt.value === value);
  if (!option) return String(value);
  return t ? t(option.labelKey) : option.labelKey;
}

export function occupancyStatusToLabel(
  value: OccupancyStatus | number | undefined | null,
  t?: (key: string) => string
): string {
  if (value === undefined || value === null) return '-';
  const option = OCCUPANCY_STATUS_OPTIONS.find((opt) => opt.value === value);
  if (!option) return String(value);
  return t ? t(option.labelKey) : option.labelKey;
}
