import { z } from 'zod';
import { OwnershipStatus } from '../constants/apartmentEnums';

export const SUPPORTED_CURRENCIES = ['JOD', 'USD', 'EUR', 'AED', 'SAR'] as const;
export type SupportedCurrency = typeof SUPPORTED_CURRENCIES[number];

export const apartmentSchema = z.object({
  floorId: z.string().min(1, 'Floor selection is required'),
  unitNumber: z.string().min(1, 'Unit number is required').max(20, 'Unit number max 20 chars'),
  areaSqm: z
    .number({ error: 'Area is required' })
    .min(0.01, 'Area must be greater than 0')
    .max(10000, 'Area max 10000 m²'),
  ownershipStatus: z.nativeEnum(OwnershipStatus),
  externalOwnerName: z.string().max(255).optional(),
  externalOwnerPhone: z.string().max(20).optional(),
  bedrooms: z.number().int().min(0).max(100).default(0),
  bathrooms: z.number().int().min(0).max(100).default(0),
  baseRentAmount: z.number().min(0).optional(),
  baseRentCurrency: z.enum(SUPPORTED_CURRENCIES).default('JOD'),
}).refine((data) => {
  if (data.ownershipStatus === OwnershipStatus.ThirdPartyOwned) {
    return !!data.externalOwnerName && data.externalOwnerName.trim().length > 0;
  }
  return true;
}, {
  message: 'External owner name is required when ownership is Third Party Owned',
  path: ['externalOwnerName'],
});

export type ApartmentFormValues = z.infer<typeof apartmentSchema>;
export type ApartmentFormInput = z.input<typeof apartmentSchema>;
