import { z } from 'zod';
import { FloorType } from '../constants/floorEnums';

export const floorSchema = z.object({
  floorNumber: z
    .number({ error: 'Floor number is required' })
    .int('Floor number must be an integer')
    .min(-5, 'Floor number min -5')
    .max(200, 'Floor number max 200'),
  floorLabel: z.string().min(1, 'Floor label is required').max(100, 'Floor label max 100 chars'),
  floorType: z.nativeEnum(FloorType),
});

export type FloorFormValues = z.infer<typeof floorSchema>;
