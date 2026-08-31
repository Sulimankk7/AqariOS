import { z } from 'zod';
import { BuildingType, Governorate } from '../constants/buildingEnums';

const addressSchema = z.object({
  governorate: z.nativeEnum(Governorate, {
    errorMap: () => ({ message: 'Please select a valid governorate' }),
  }),
  district: z.string().trim().min(1, 'City is required').max(100, 'City is too long'),
  area: z.string().trim().min(1, 'Area is required').max(100, 'Area is too long'),
  streetName: z.string().max(100, 'Street name is too long').optional(),
  postalCode: z.string().max(20, 'Postal code is too long').optional(),
});

export const buildingSchema = z.object({
  name: z.string().min(1, 'Building name is required').max(100, 'Building name is too long'),
  internalCode: z.string().max(50, 'Internal code is too long').optional().or(z.literal('')),
  buildingType: z.nativeEnum(BuildingType, {
    errorMap: () => ({ message: 'Please select a valid building type' }),
  }),
  totalFloors: z
    .number({ invalid_type_error: 'Total floors is required' })
    .int('Total floors must be a whole number')
    .min(1, 'Building must have at least 1 floor')
    .max(200, 'Too many floors'),
  constructionYear: z
    .number()
    .int('Construction year must be a whole number')
    .min(1800, 'Year must be valid')
    .max(new Date().getFullYear() + 5, 'Year cannot be too far in the future')
    .optional(),
  gpsLatitude: z
    .number()
    .min(-90, 'Latitude must be between -90 and 90')
    .max(90, 'Latitude must be between -90 and 90')
    .optional(),
  gpsLongitude: z
    .number()
    .min(-180, 'Longitude must be between -180 and 180')
    .max(180, 'Longitude must be between -180 and 180')
    .optional(),
  address: addressSchema,
});

export type BuildingFormValues = z.infer<typeof buildingSchema>;
