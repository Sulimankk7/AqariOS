/**
 * Tenant Portal Types — Synchronized 1:1 with ASP.NET Core Backend DTOs.
 * Source DTO: PropertyOS.Application.Leasing.Queries.GetTenantById.TenantDetailDto
 */

export interface TenantFamilyMemberDto {
  id: string;
  name: string;
  relationshipType: string;
  ageBracket?: string | null;
  createdAt: string;
}

export interface TenantEmergencyContactDto {
  id: string;
  name: string;
  relationshipType: string;
  phone: string;
  createdAt: string;
}

export interface TenantVehicleDto {
  id: string;
  plateNumber: string;
  makeModel: string;
  color: string;
  createdAt: string;
}

export interface TenantDetailDto {
  id: string;
  companyId: string;
  name: string;
  nationalId: string;
  phone: string;
  email?: string | null;
  occupation?: string | null;
  employer?: string | null;
  userId?: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy?: string | null;
  updatedBy?: string | null;
  familyMembers: TenantFamilyMemberDto[];
  emergencyContacts: TenantEmergencyContactDto[];
  vehicles: TenantVehicleDto[];
}

/**
 * Tenant Active Lease DTO — Synchronized 1:1 with ASP.NET Core Backend DTO.
 * Source DTO: PropertyOS.Application.Leasing.Queries.GetMyActiveLease.TenantLeaseDto
 */
export interface TenantLeaseDto {
  id: string;
  contractNumber: string;
  startDate: string;
  endDate: string;
  signedDate?: string | null;
  monthlyRentAmount: number;
  currency: string;
  securityDepositAmount: number;
  paymentFrequency: string;
  paymentDueDay: number;
  status: string;
  legalRegime: string;
  tenantType: string;
  apartmentId: string;
  apartmentUnitNumber: string;
  apartmentBedrooms: number;
  apartmentBathrooms: number;
  apartmentAreaSqm: number;
  buildingId: string;
  buildingName: string;
}

/**
 * Payment method options matching backend enum PropertyOS.Domain.Financials.Enums.PaymentMethod
 */
export enum PaymentMethod {
  Cash = "Cash",
  BankTransfer = "BankTransfer",
  Cheque = "Cheque",
  Efawateercom = "Efawateercom",
  CliQ = "CliQ",
}

export interface ChequeSubmissionInput {
  chequeNumber: string;
  bankName: string;
  issueDate: string;
  dueDate: string;
}

/**
 * Request payload for POST /api/v1/tenant/payments/{id}/submit-verification
 * Synchronized 1:1 with backend DTO SubmitPaymentVerificationRequest
 */
export interface SubmitPaymentVerificationRequestPayload {
  amount: number;
  paymentMethod: number | PaymentMethod | string;
  referenceNumber?: string | null;
  proofFileId?: string | null;
  chequeDetails?: ChequeSubmissionInput | null;
}

/**
 * Payment due-date status — synchronized 1:1 with
 * PropertyOS.Domain.Financials.Enums.DueDateStatus
 */
export enum DueDateStatus {
  Pending = "Pending",
  PendingVerification = "PendingVerification",
  Paid = "Paid",
  PartiallyPaid = "PartiallyPaid",
  Late = "Late",
  OverdueUnpaid = "OverdueUnpaid",
  Cancelled = "Cancelled",
}

/**
 * Payment purpose — synchronized 1:1 with
 * PropertyOS.Domain.Financials.Enums.PaymentPurpose
 */
export enum PaymentPurpose {
  ScheduledInstallment = "ScheduledInstallment",
  UnallocatedReceipt = "UnallocatedReceipt",
  AdjustmentCredit = "AdjustmentCredit",
  AdjustmentDebit = "AdjustmentDebit",
}

/**
 * Payment submission status — synchronized 1:1 with
 * PropertyOS.Domain.Financials.Enums.SubmissionStatus
 */
export enum SubmissionStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
}

/**
 * Tenant payment list item — synchronized 1:1 with
 * PropertyOS.Application.Financials.Queries.Common.RentPaymentDto
 * Returned by GET /api/v1/tenant-portal/payments
 */
export interface TenantPaymentDto {
  id: string;
  companyId: string;
  leaseContractId: string;
  tenantId: string;
  buildingId: string;
  apartmentId: string;

  paymentPurpose: PaymentPurpose | string;
  amountDue: number;
  amountPaid: number;
  currency: string;
  dueDateStatus: DueDateStatus | string;

  billingPeriodStart?: string | null;
  billingPeriodEnd?: string | null;
  dueDate?: string | null;

  receiptNumber?: string | null;
  paymentMethod?: PaymentMethod | string | null;
  paymentReferenceNumber?: string | null;
  notes?: string | null;

  createdAt: string;
  updatedAt: string;
  createdBy?: string | null;
  updatedBy?: string | null;

  // Read-side display enrichment from join projection
  tenantName?: string | null;
  buildingName?: string | null;
  apartmentNumber?: string | null;
  contractNumber?: string | null;

  // Latest Payment Submission projection (for Tenant Portal visibility)
  latestSubmissionStatus?: SubmissionStatus | string | number | null;
  latestSubmissionRejectionReason?: string | null;
  latestSubmissionAmount?: number | null;
  latestSubmissionDate?: string | null;

  // Receipt File ID (for direct download)
  receiptFileId?: string | null;

  // Hybrid Receipt Model: transaction receipts and settlement summary
  transactionReceipts?: TransactionReceiptDto[];
  settlementSummary?: SettlementStatementSummaryDto | null;
}

export interface TransactionReceiptDto {
  receiptId: string;
  receiptNumber: string;
  amount: number;
  issuedAt: string;
  fileId?: string | null;
  paymentMethod?: PaymentMethod | string | null;
  referenceNumber?: string | null;
  previouslyPaid: number;
  remainingAfter: number;
}

export interface SettlementStatementSummaryDto {
  isAvailable: boolean;
  totalDue: number;
  totalPaid: number;
  remaining: number;
  transactionCount: number;
  settledAt?: string | null;
}

