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
  Adjustment = 2,
}

export enum AllocationStatus {
  Active = 0,
  Reversed = 1,
}

export enum ExpenseCategory {
  Building = 0,
  Shared = 1,
  Emergency = 2,
  UtilityCommonArea = 3,
  Maintenance = 4,
  Cleaning = 5,
  Security = 6,
  Elevator = 7,
  WaterTank = 8,
  Generator = 9,
  Administrative = 10,
  Other = 11,
}

export enum ExpensePaymentMethod {
  Cash = 0,
  BankTransfer = 1,
  Cheque = 2,
  Other = 3,
}

export interface TransactionReceiptDto {
  receiptId: string;
  receiptNumber: string;
  amount: number;
  issuedAt: string;
  fileId?: string | null;
  paymentMethod?: PaymentMethod | string | number | null;
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

export interface PaymentAllocationDto {
  id: string;
  companyId: string;
  receivingPaymentId: string;
  obligationPaymentId: string;
  allocatedAmount: number;
  allocationDate: string;
  allocationStatus: AllocationStatus | string | number;
  reversalReason?: string | null;
  reversedAt?: string | null;
  reversedBy?: string | null;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy?: string | null;
  updatedBy?: string | null;
}

export interface PaymentSubmissionDto {
  id: string;
  amount?: number | null;
  paymentMethod: PaymentMethod | string | number;
  referenceNumber?: string | null;
  proofFileId?: string | null;
  chequeNumber?: string | null;
  bankName?: string | null;
  chequeIssueDate?: string | null;
  chequeDueDate?: string | null;
  status: string | number;
  submittedAt: string;
  verifiedAt?: string | null;
  rejectionReason?: string | null;
}

export interface ChequeDetailDto {
  id: string;
  chequeNumber: string;
  bankName: string;
  bankBranch?: string | null;
  issueDate: string;
  dueDate: string;
  amount: number;
  currency: string;
  status: string | number;
  receivedDate?: string | null;
  depositDate?: string | null;
  clearanceDate?: string | null;
  bounceDate?: string | null;
  bounceReason?: string | null;
  cancellationReason?: string | null;
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

  latestSubmissionStatus?: string | number | null;
  latestSubmissionRejectionReason?: string | null;
  latestSubmissionAmount?: number | null;
  latestSubmissionDate?: string | null;
  receiptFileId?: string | null;
  transactionReceipts?: TransactionReceiptDto[];
  settlementSummary?: SettlementStatementSummaryDto | null;
}

export interface RentPaymentDetailDto extends RentPaymentDto {
  chequeDetails?: ChequeDetailDto | null;
  incomingAllocations?: PaymentAllocationDto[];
  outgoingAllocations?: PaymentAllocationDto[];
  submissions?: PaymentSubmissionDto[];
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

export interface ExpenseDto {
  id: string;
  companyId: string;
  buildingId?: string | null;
  category: ExpenseCategory | string | number;
  amount: number;
  currency: string;
  expenseDate: string;
  paymentMethod: ExpensePaymentMethod | string | number;
  vendorName?: string | null;
  invoiceNumber?: string | null;
  description: string;
  notes?: string | null;
  createdAt: string;
}

export interface ExpenseReceiptDto {
  id: string;
  companyId: string;
  expenseId: string;
  fileId: string;
  receiptNumber: string;
  amount: number;
  issuedAt: string;
  description?: string | null;
  createdAt: string;
}

export interface ExpenseDetailDto extends ExpenseDto {
  receipts: ExpenseReceiptDto[];
}

export interface ExpenseFilterParams {
  buildingId?: string | null;
  category?: ExpenseCategory | number | string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  lastSeenId?: string | null;
  lastSeenExpenseDate?: string | null;
  pageSize?: number;
}
