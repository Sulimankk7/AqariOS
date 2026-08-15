import { z } from 'zod';

export const E164_PHONE_REGEX = /^\+[1-9]\d{6,14}$/;

export const createTenantSchema = z.object({
  name: z
    .string()
    .min(1, { message: 'Full name is required' })
    .max(255, { message: 'Name must not exceed 255 characters' }),
  nationalId: z
    .string()
    .min(1, { message: 'National ID / Passport number is required' })
    .max(50, { message: 'National ID must not exceed 50 characters' }),
  phone: z
    .string()
    .min(1, { message: 'Phone number is required' })
    .max(20, { message: 'Phone must not exceed 20 characters' })
    .refine((val) => E164_PHONE_REGEX.test(val.trim()), {
      message: 'Phone must be a valid E.164 phone number (e.g. +962791234567)',
    }),
  email: z
    .string()
    .min(1, { message: 'Email address is required' })
    .email({ message: 'Please enter a valid email address' })
    .max(255, { message: 'Email must not exceed 255 characters' }),
  occupation: z
    .string()
    .max(100, { message: 'Occupation must not exceed 100 characters' })
    .optional()
    .nullable(),
  employer: z
    .string()
    .max(100, { message: 'Employer must not exceed 100 characters' })
    .optional()
    .nullable(),
});

export const updateTenantSchema = z.object({
  name: z
    .string()
    .min(1, { message: 'Full name is required' })
    .max(255, { message: 'Name must not exceed 255 characters' }),
  nationalId: z
    .string()
    .min(1, { message: 'National ID / Passport number is required' })
    .max(50, { message: 'National ID must not exceed 50 characters' }),
  phone: z
    .string()
    .min(1, { message: 'Phone number is required' })
    .max(20, { message: 'Phone must not exceed 20 characters' })
    .refine((val) => E164_PHONE_REGEX.test(val.trim()), {
      message: 'Phone must be a valid E.164 phone number (e.g. +962791234567)',
    }),
  email: z
    .string()
    .email({ message: 'Please enter a valid email address' })
    .max(255, { message: 'Email must not exceed 255 characters' })
    .optional()
    .nullable()
    .or(z.literal('')),
  occupation: z
    .string()
    .max(100, { message: 'Occupation must not exceed 100 characters' })
    .optional()
    .nullable(),
  employer: z
    .string()
    .max(100, { message: 'Employer must not exceed 100 characters' })
    .optional()
    .nullable(),
});

export type CreateTenantFormValues = z.infer<typeof createTenantSchema>;
export type UpdateTenantFormValues = z.infer<typeof updateTenantSchema>;

