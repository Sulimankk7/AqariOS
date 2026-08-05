import {
  ContractStatus,
  ContractDocumentType,
  LegalRegime,
  PaymentFrequency,
  TenantType,
  TerminationType,
} from '../types/leasing.types';

export function contractStatusToLabel(status: ContractStatus, t: (key: string) => string): string {
  switch (status) {
    case ContractStatus.Draft: return t('statusDraft');
    case ContractStatus.PendingSignature: return t('statusPendingSignature');
    case ContractStatus.Active: return t('statusActive');
    case ContractStatus.Expired: return t('statusExpired');
    case ContractStatus.Renewed: return t('statusRenewed');
    case ContractStatus.Terminated: return t('statusTerminated');
    case ContractStatus.Cancelled: return t('statusCancelled');
    case ContractStatus.Superseded: return t('statusSuperseded');
    default: return String(status);
  }
}

export function contractStatusToBadgeVariant(status: ContractStatus): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case ContractStatus.Active: return 'default';
    case ContractStatus.Draft: return 'secondary';
    case ContractStatus.PendingSignature: return 'outline';
    case ContractStatus.Expired: return 'destructive';
    case ContractStatus.Renewed: return 'secondary';
    case ContractStatus.Terminated: return 'destructive';
    case ContractStatus.Cancelled: return 'outline';
    case ContractStatus.Superseded: return 'outline';
    default: return 'secondary';
  }
}

export function contractDocumentTypeToLabel(type: ContractDocumentType, t: (key: string) => string): string {
  switch (type) {
    case ContractDocumentType.SignedContract: return t('docSignedContract');
    case ContractDocumentType.NationalIdCopy: return t('docNationalIdCopy');
    case ContractDocumentType.Passport: return t('docPassport');
    case ContractDocumentType.IncomeProof: return t('docIncomeProof');
    case ContractDocumentType.Other: return t('docOther');
    default: return String(type);
  }
}

export function legalRegimeToLabel(regime: LegalRegime, t: (key: string) => string): string {
  switch (regime) {
    case LegalRegime.Standard: return t('legalStandard');
    case LegalRegime.OldRentLaw: return t('legalOldRentLaw');
    default: return String(regime);
  }
}

export function paymentFrequencyToLabel(frequency: PaymentFrequency, t: (key: string) => string): string {
  switch (frequency) {
    case PaymentFrequency.Monthly: return t('freqMonthly');
    case PaymentFrequency.Quarterly: return t('freqQuarterly');
    case PaymentFrequency.SemiAnnual: return t('freqSemiAnnual');
    case PaymentFrequency.Annual: return t('freqAnnual');
    default: return String(frequency);
  }
}

export function tenantTypeToLabel(type: TenantType, t: (key: string) => string): string {
  switch (type) {
    case TenantType.Personal: return t('tenantPersonal');
    case TenantType.Corporate: return t('tenantCorporate');
    default: return String(type);
  }
}

export function terminationTypeToLabel(type: TerminationType, t: (key: string) => string): string {
  switch (type) {
    case TerminationType.NormalExpiration: return t('termNormalExpiration');
    case TerminationType.EarlyTermination: return t('termEarlyTermination');
    case TerminationType.MutualAgreement: return t('termMutualAgreement');
    case TerminationType.TenantRequest: return t('termTenantRequest');
    case TerminationType.OwnerRequest: return t('termOwnerRequest');
    case TerminationType.LegalEviction: return t('termLegalEviction');
    default: return String(type);
  }
}
