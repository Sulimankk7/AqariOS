import { z } from 'zod';
import { TENANT_PHONE_INPUT_REGEX } from './tenants.schema';

export const emergencyContactSchema = z.object({
  name: z
    .string()
    .min(1, { message: 'nameRequired' })
    .max(255, { message: 'nameMax255' }),
  relationshipType: z
    .string()
    .min(1, { message: 'relationshipTypeRequired' })
    .max(50, { message: 'relationshipTypeMax50' }),
  phone: z
    .string()
    .min(1, { message: 'phoneRequired' })
    .max(20, { message: 'phoneMax20' })
    .regex(TENANT_PHONE_INPUT_REGEX, { message: 'invalidPhoneFormat' }),
});

export type EmergencyContactFormData = z.infer<typeof emergencyContactSchema>;
