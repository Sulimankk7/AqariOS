import { z } from 'zod';

export const createTenantSchema = z.object({
  name: z.string().min(1, { message: 'Full name is required' }),
  nationalId: z.string().min(1, { message: 'National ID / Passport number is required' }),
  phone: z.string().min(1, { message: 'Phone number is required' }),
  occupation: z.string().optional().nullable(),
  employer: z.string().optional().nullable(),
});

export const updateTenantSchema = z.object({
  name: z.string().min(1, { message: 'Full name is required' }),
  nationalId: z.string().min(1, { message: 'National ID / Passport number is required' }),
  phone: z.string().min(1, { message: 'Phone number is required' }),
  occupation: z.string().optional().nullable(),
  employer: z.string().optional().nullable(),
});

export type CreateTenantFormValues = z.infer<typeof createTenantSchema>;
export type UpdateTenantFormValues = z.infer<typeof updateTenantSchema>;
