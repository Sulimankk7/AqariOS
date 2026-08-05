import { z } from 'zod';
import { 
  LegalRegime, 
  PaymentFrequency, 
  TenantType, 
  TerminationType, 
  ContractDocumentType 
} from '../types/leasing.types';

export const createLeaseContractSchema = z.object({
  apartmentId: z.string().uuid({ message: 'Apartment selection is required' }),
  tenantId: z.string().uuid({ message: 'Tenant selection is required' }),
  contractNumber: z.string().min(1, { message: 'Contract number is required' }),
  startDate: z.string().min(1, { message: 'Start date is required' }),
  endDate: z.string().min(1, { message: 'End date is required' }),
  monthlyRentAmount: z.coerce.number().positive({ message: 'Monthly rent must be greater than zero' }),
  securityDepositAmount: z.coerce.number().min(0, { message: 'Security deposit cannot be negative' }),
  paymentFrequency: z.nativeEnum(PaymentFrequency),
  paymentDueDay: z.coerce.number().int().min(1).max(31, { message: 'Payment due day must be between 1 and 31' }),
  legalRegime: z.nativeEnum(LegalRegime).default(LegalRegime.Standard),
  tenantType: z.nativeEnum(TenantType).default(TenantType.Personal),
  notes: z.string().optional().nullable(),
});

export const updateDraftLeaseContractSchema = z.object({
  apartmentId: z.string().uuid({ message: 'Apartment selection is required' }),
  tenantId: z.string().uuid({ message: 'Tenant selection is required' }),
  startDate: z.string().min(1, { message: 'Start date is required' }),
  endDate: z.string().min(1, { message: 'End date is required' }),
  monthlyRentAmount: z.coerce.number().positive({ message: 'Monthly rent must be greater than zero' }),
  securityDepositAmount: z.coerce.number().min(0, { message: 'Security deposit cannot be negative' }),
  paymentFrequency: z.nativeEnum(PaymentFrequency),
  paymentDueDay: z.coerce.number().int().min(1).max(31, { message: 'Payment due day must be between 1 and 31' }),
  legalRegime: z.nativeEnum(LegalRegime).default(LegalRegime.Standard),
  tenantType: z.nativeEnum(TenantType).default(TenantType.Personal),
  notes: z.string().optional().nullable(),
});

export const renewLeaseContractSchema = z.object({
  contractNumber: z.string().min(1, { message: 'New contract number is required' }),
  startDate: z.string().min(1, { message: 'Start date is required' }),
  endDate: z.string().min(1, { message: 'End date is required' }),
  monthlyRentAmount: z.coerce.number().positive({ message: 'Monthly rent must be greater than zero' }),
  securityDepositAmount: z.coerce.number().min(0, { message: 'Security deposit cannot be negative' }),
  paymentFrequency: z.nativeEnum(PaymentFrequency),
  paymentDueDay: z.coerce.number().int().min(1).max(31, { message: 'Payment due day must be between 1 and 31' }),
  legalRegime: z.nativeEnum(LegalRegime).default(LegalRegime.Standard),
  tenantType: z.nativeEnum(TenantType).default(TenantType.Personal),
  notes: z.string().optional().nullable(),
});

export const terminateLeaseContractSchema = z.object({
  terminationType: z.nativeEnum(TerminationType),
  terminationDate: z.string().min(1, { message: 'Termination date is required' }),
  outstandingBalance: z.coerce.number().min(0).default(0),
  depositReturnedAmount: z.coerce.number().min(0).default(0),
  depositDeductionAmount: z.coerce.number().min(0).default(0),
  depositDeductionReason: z.string().optional().nullable(),
  finalUtilitySettlementCompleted: z.boolean().default(false),
  reason: z.string().optional().nullable(),
  notes: z.string().optional().nullable(),
});

export const attachContractDocumentSchema = z.object({
  fileId: z.string().uuid({ message: 'Valid file ID is required' }),
  documentType: z.nativeEnum(ContractDocumentType),
  description: z.string().optional().nullable(),
});

export type CreateLeaseContractFormValues = z.infer<typeof createLeaseContractSchema>;
export type UpdateDraftLeaseContractFormValues = z.infer<typeof updateDraftLeaseContractSchema>;
export type RenewLeaseContractFormValues = z.infer<typeof renewLeaseContractSchema>;
export type TerminateLeaseContractFormValues = z.infer<typeof terminateLeaseContractSchema>;
export type AttachContractDocumentFormValues = z.infer<typeof attachContractDocumentSchema>;
