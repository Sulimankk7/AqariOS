import { z } from 'zod';

export const TENANT_PHONE_INPUT_REGEX = /^[+0-9\s()./-]+$/;

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
    .refine((val) => TENANT_PHONE_INPUT_REGEX.test(val.trim()), {
      message: 'Phone contains invalid characters',
    }),
  phoneCountryCode: z.string().length(2).optional().or(z.literal('')),
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
}).superRefine((value, context) => {
  const compact = value.phone.replace(/[\s()./-]/g, '');
  if (!compact.startsWith('+') && !compact.startsWith('00') && !value.phoneCountryCode) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['phoneCountryCode'],
      message: 'Select a country when entering a local phone number',
    });
  }
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
    .refine((val) => TENANT_PHONE_INPUT_REGEX.test(val.trim()), {
      message: 'Phone contains invalid characters',
    }),
  phoneCountryCode: z.string().length(2).optional().or(z.literal('')),
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
}).superRefine((value, context) => {
  const compact = value.phone.replace(/[\s()./-]/g, '');
  if (!compact.startsWith('+') && !compact.startsWith('00') && !value.phoneCountryCode) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['phoneCountryCode'],
      message: 'Select a country when entering a local phone number',
    });
  }
});

export type CreateTenantFormValues = z.infer<typeof createTenantSchema>;
export type UpdateTenantFormValues = z.infer<typeof updateTenantSchema>;
