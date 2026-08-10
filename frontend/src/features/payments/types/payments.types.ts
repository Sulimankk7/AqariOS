export interface PaymentVerificationQueueItem {
  rentPaymentId: string;
  paymentSubmissionId: string;
  tenantId: string;
  tenantName: string | null;
  buildingId: string | null;
  buildingName: string | null;
  apartmentId: string | null;
  apartmentNumber: string | null;
  leaseContractId: string | null;
  contractNumber: string | null;
  amountDue: number;
  amountPaid: number;
  currency: string;
  paymentMethod: string;
  referenceNumber: string | null;
  proofFileId: string | null;
  submittedAt: string;
  submissionStatus: string;
  dueDateStatus: string;
}

export interface KeysetPage<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}

export interface PaymentSubmissionDto {
  id: string;
  paymentMethod: string;
  referenceNumber: string | null;
  proofFileId: string | null;
  status: string;
  submittedAt: string;
  verifiedAt: string | null;
  rejectionReason: string | null;
}

export interface RentPaymentDetail {
  id: string;
  companyId: string;
  leaseContractId: string;
  tenantId: string;
  buildingId: string | null;
  apartmentId: string | null;
  paymentPurpose: string;
  amountDue: number;
  amountPaid: number;
  currency: string;
  dueDateStatus: string;
  billingPeriodStart: string | null;
  billingPeriodEnd: string | null;
  dueDate: string | null;
  receiptNumber: string | null;
  notes: string | null;
  submissions: PaymentSubmissionDto[];
}
