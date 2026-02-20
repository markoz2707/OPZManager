import { z } from 'zod';

export const createManufacturerSchema = z.object({
  name: z.string().min(1, 'Nazwa producenta jest wymagana').max(100, 'Nazwa nie może przekraczać 100 znaków'),
  description: z.string().max(500, 'Opis nie może przekraczać 500 znaków').default(''),
});

export const createEquipmentTypeSchema = z.object({
  name: z.string().min(1, 'Nazwa typu sprzętu jest wymagana').max(100, 'Nazwa nie może przekraczać 100 znaków'),
  description: z.string().max(500, 'Opis nie może przekraczać 500 znaków').default(''),
});

export const createEquipmentModelSchema = z.object({
  manufacturerId: z.number().min(1, 'Producent jest wymagany'),
  typeId: z.number().min(1, 'Typ sprzętu jest wymagany'),
  modelName: z.string().min(1, 'Nazwa modelu jest wymagana').max(200, 'Nazwa nie może przekraczać 200 znaków'),
  specificationsJson: z.string().default('{}'),
});

export type CreateManufacturerInput = z.infer<typeof createManufacturerSchema>;
export type CreateEquipmentTypeInput = z.infer<typeof createEquipmentTypeSchema>;
export type CreateEquipmentModelInput = z.infer<typeof createEquipmentModelSchema>;
