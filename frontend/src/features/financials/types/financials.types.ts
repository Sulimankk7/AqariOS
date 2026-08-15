/**
 * Financial Operations Types.
 * Strictly models the ASP.NET Core backend DTOs and query contracts for Module 6/7.
 */

export enum DueDateStatus {
  Pending = 0,
  PendingVerification = 1,
  Paid = 2,
  PartiallyPaid = 3,
  Late = 4,
  OverdueUnpaid = 5,
  Cancelled = 6,
}

export enum PaymentMethod {
  Cash = 0,
  BankTransfer = 1,
  Cheque = 2,
  Efawateercom = 3,
  CliQ = 4,
}

export enum PaymentPurpose {
  ScheduledInstallment = 0,
  UnallocatedReceipt = 1,
  AdjustmentCredit = 2,
  AdjustmentDebit = 3,
}

export interface RentPaymentDto {
  id: string;
  companyId: string;
  leaseContractId: string;
  tenantId: string;
  buildingId: string;
  apartmentId: string;
  paymentPurpose: PaymentPurpose | string | number;
  amountDue: number;
  amountPaid: number;
  currency: string;
  dueDateStatus: DueDateStatus | string | number;
  billingPeriodStart: string | null;
  billingPeriodEnd: string | null;
  dueDate: string | null;
  receiptNumber: string | null;
  paymentMethod: PaymentMethod | string | number | null;
  paymentReferenceNumber: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy: string | null;
  updatedBy: string | null;

  // Read-side joined display properties
  tenantName: string | null;
  buildingName: string | null;
  apartmentNumber: string | null;
  contractNumber: string | null;
}

export interface RentPaymentFilterParams {
  buildingId?: string | null;
  status?: DueDateStatus | number | string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  searchTerm?: string | null;
  lastSeenId?: string | null;
  lastSeenDueDate?: string | null;
  pageSize?: number;
}

export interface RemindRentPaymentResponseDto {
  rentPaymentId: string;
  notificationId: string;
  status: string;
  message: string;
}
