import { z } from 'zod';

export const vehicleSchema = z.object({
  plateNumber: z
    .string()
    .min(1, { message: 'plateNumberRequired' })
    .max(20, { message: 'plateNumberMax20' }),
  makeModel: z
    .string()
    .min(1, { message: 'makeModelRequired' })
    .max(100, { message: 'makeModelMax100' }),
  color: z
    .string()
    .min(1, { message: 'colorRequired' })
    .max(50, { message: 'colorMax50' }),
});

export type VehicleFormData = z.infer<typeof vehicleSchema>;
