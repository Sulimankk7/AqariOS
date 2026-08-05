export enum ContractStatus {
  Draft = 0,
  PendingSignature = 1,
  Active = 2,
  Expired = 3,
  Renewed = 4,
  Terminated = 5,
  Cancelled = 6,
  Superseded = 7,
}

export enum ContractDocumentType {
  SignedContract = 0,
  NationalIdCopy = 1,
  Passport = 2,
  IncomeProof = 3,
  Other = 4,
}

export enum LegalRegime {
  Standard = 0,
  OldRentLaw = 1,
}

export enum PaymentFrequency {
  Monthly = 0,
  Quarterly = 1,
  SemiAnnual = 2,
  Annual = 3,
}

export enum TenantType {
  Personal = 0,
  Corporate = 1,
}

export enum TerminationType {
  NormalExpiration = 0,
  EarlyTermination = 1,
  MutualAgreement = 2,
  TenantRequest = 3,
  OwnerRequest = 4,
  LegalEviction = 5,
}

export interface LeaseContractDto {
  id: string;
  companyId: string;
  buildingId: string;
  apartmentId: string;
  tenantId: string;
  priorContractId?: string | null;
  contractNumber: string;
  legalRegime: LegalRegime;
  tenantType: TenantType;
  startDate: string;
  endDate: string;
  signedDate?: string | null;
  monthlyRentAmount: number;
  currency: string;
  securityDepositAmount: number;
  paymentFrequency: PaymentFrequency;
  paymentDueDay: number;
  status: ContractStatus;
  externalRegistrationRef?: string | null;
  contractDocumentId?: string | null;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy?: string | null;
  updatedBy?: string | null;
}

export interface ContractStatusHistoryDto {
  id: string;
  companyId: string;
  leaseContractId: string;
  previousStatus?: ContractStatus | null;
  fromStatus?: ContractStatus | null;
  newStatus?: ContractStatus;
  toStatus: ContractStatus;
  changedBy?: string | null;
  changedAt: string;
  reason?: string | null;
}

export interface ContractDocumentDto {
  id: string;
  companyId: string;
  leaseContractId: string;
  fileId: string;
  documentType: ContractDocumentType;
  description?: string | null;
  originalFilename?: string | null;
  mimeType?: string | null;
  sizeBytes: number;
  uploadedBy?: string | null;
  createdAt: string;
}

export interface ReplaceContractDocumentRequest {
  newFileId: string;
  description?: string | null;
}


export interface ContractTerminationDto {
  id: string;
  companyId: string;
  leaseContractId: string;
  terminationType: TerminationType;
  terminationDate: string;
  reason?: string | null;
  notes?: string | null;
  approvedBy?: string | null;
  outstandingBalance: number;
  depositReturnedAmount: number;
  depositDeductionAmount: number;
  depositDeductionReason?: string | null;
  finalUtilitySettlementCompleted: boolean;
  currency: string;
  createdAt: string;
}

export interface LeaseContractDetailDto extends LeaseContractDto {
  statusHistory: ContractStatusHistoryDto[];
  documents: ContractDocumentDto[];
  termination?: ContractTerminationDto | null;
}

export interface CreateLeaseContractRequest {
  apartmentId: string;
  tenantId: string;
  contractNumber: string;
  startDate: string;
  endDate: string;
  monthlyRentAmount: number;
  securityDepositAmount: number;
  paymentFrequency: PaymentFrequency;
  paymentDueDay: number;
  legalRegime?: LegalRegime;
  tenantType?: TenantType;
  notes?: string | null;
}

export interface UpdateDraftLeaseContractRequest {
  apartmentId: string;
  tenantId: string;
  startDate: string;
  endDate: string;
  monthlyRentAmount: number;
  securityDepositAmount: number;
  paymentFrequency: PaymentFrequency;
  paymentDueDay: number;
  legalRegime?: LegalRegime;
  tenantType?: TenantType;
  notes?: string | null;
}

export interface RenewLeaseContractRequest {
  contractNumber: string;
  startDate: string;
  endDate: string;
  monthlyRentAmount: number;
  securityDepositAmount: number;
  paymentFrequency: PaymentFrequency;
  paymentDueDay: number;
  legalRegime?: LegalRegime;
  tenantType?: TenantType;
  notes?: string | null;
}

export interface TerminateLeaseContractRequest {
  terminationType: TerminationType;
  terminationDate: string;
  outstandingBalance?: number;
  depositReturnedAmount?: number;
  depositDeductionAmount?: number;
  depositDeductionReason?: string | null;
  finalUtilitySettlementCompleted?: boolean;
  reason?: string | null;
  notes?: string | null;
}

export interface AttachContractDocumentRequest {
  fileId: string;
  documentType: ContractDocumentType;
  description?: string | null;
}

export interface TenantLookupDto {
  id: string;
  name: string;
  nationalId: string;
  phone: string;
  occupation?: string | null;
  employer?: string | null;
}

export interface ContractDocumentDownloadUrlDto {
  url: string;
  filename: string;
}

