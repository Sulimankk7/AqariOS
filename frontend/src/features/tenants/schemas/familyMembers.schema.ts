import { z } from 'zod';

export const familyMemberSchema = z.object({
  name: z
    .string()
    .min(1, { message: 'nameRequired' })
    .max(255, { message: 'nameMax255' }),
  relationshipType: z
    .string()
    .min(1, { message: 'relationshipTypeRequired' })
    .max(50, { message: 'relationshipTypeMax50' }),
  ageBracket: z
    .string()
    .max(30, { message: 'ageBracketMax30' })
    .optional()
    .nullable()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : null)),
});

export type FamilyMemberFormData = z.infer<typeof familyMemberSchema>;
export type FamilyMemberFormInput = z.input<typeof familyMemberSchema>;
