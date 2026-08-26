import React from 'react';
import {
  Bell, Building2, Calendar, CheckCircle2, ChevronRight, CreditCard, Download,
  FileText, Home, Landmark, Loader2, Receipt, User, Wallet, X,
} from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/app/components/ui/button';
import { ErrorState, Skeleton } from '@/shared/components/ui/Feedback';
import { StatusBadge, type StatusVariant } from '@/shared/components/ui/StatusBadge';
import { useTranslation } from '@/shared/i18n';
import { filesApi } from '@/shared/services/files.api';
import { useTenantDetails } from '@/features/tenants/hooks/useTenants';
import { useLeaseDetails } from '@/features/leasing/hooks/useLeasing';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { useApartment } from '@/features/apartments/hooks/useApartments';
import { BuildingType } from '@/features/buildings/types/buildings.types';
import { OccupancyStatus, OwnershipStatus } from '@/features/apartments/types/apartments.types';
import { ContractStatus, PaymentFrequency } from '@/features/leasing/types/leasing.types';
import { financialsApi } from '../api/financials.api';
import { usePaymentDetails, useRemindRentPayment } from '../hooks/useRentPayments';
import {
  AllocationStatus, DueDateStatus, PaymentMethod, PaymentPurpose,
  type PaymentAllocationDto, type RentPaymentDto,
} from '../types/financials.types';
import {
  allocationStatusValue, dueDateStatusValue, formatFinancialCurrency, formatFinancialDate,
  formatFinancialNumber, isReceivedPayment, isScheduledInstallment, paymentMethodValue,
  paymentPurposeValue,
} from '../utils/financialRecord';

interface FinancialPaymentDetailsDrawerProps {
  payment: RentPaymentDto | null;
  onClose: () => void;
}

type DrawerView = 'record' | 'tenant' | 'lease' | 'building' | 'apartment';

export function FinancialPaymentDetailsDrawer({ payment, onClose }: FinancialPaymentDetailsDrawerProps) {
  const { t, language } = useTranslation();
  const [view, setView] = React.useState<DrawerView>('record');
  const { data: details, isLoading, isError, refetch } = usePaymentDetails(payment?.id || null);
  const remindMutation = useRemindRentPayment();
  const tenantQuery = useTenantDetails(view === 'tenant' ? payment?.tenantId || '' : '');
  const leaseQuery = useLeaseDetails(view === 'lease' ? payment?.leaseContractId || '' : '');
  const buildingQuery = useBuilding(view === 'building' ? payment?.buildingId || '' : '');
  const apartmentQuery = useApartment(view === 'apartment' ? payment?.apartmentId || '' : '');
  const [downloadingId, setDownloadingId] = React.useState<string | null>(null);
  const [isDownloadingSettlement, setIsDownloadingSettlement] = React.useState(false);

  React.useEffect(() => setView('record'), [payment?.id]);

  if (!payment) return null;

  const record = details ? {
    ...payment,
    ...details,
    tenantName: details.tenantName || payment.tenantName,
    contractNumber: details.contractNumber || payment.contractNumber,
    buildingName: details.buildingName || payment.buildingName,
    apartmentNumber: details.apartmentNumber || payment.apartmentNumber,
    transactionReceipts: details.transactionReceipts?.length ? details.transactionReceipts : payment.transactionReceipts,
    settlementSummary: details.settlementSummary ?? payment.settlementSummary,
    receiptFileId: details.receiptFileId ?? payment.receiptFileId,
  } : payment;
  const obligation = isScheduledInstallment(record.paymentPurpose);
  const received = isReceivedPayment(record.paymentPurpose);
  const summary = record.settlementSummary;
  const remaining = summary?.remaining;
  const isOutstanding = obligation && remaining != null && remaining > 0 &&
    dueDateStatusValue(record.dueDateStatus) !== DueDateStatus.Paid &&
    dueDateStatusValue(record.dueDateStatus) !== DueDateStatus.Cancelled;

  const purposeConfig = getPurposeConfig(record.paymentPurpose, t);
  const statusConfig = getStatusConfig(record.dueDateStatus, t);
  const displayStatus = obligation ? statusConfig : purposeConfig;
  const titleByView: Record<Exclude<DrawerView, 'record'>, string> = {
    tenant: t('financials.tenantDetails'),
    lease: t('financials.leaseDetails'),
    building: t('financials.buildingDetails'),
    apartment: t('financials.unitDetails'),
  };

  const downloadReceipt = async (fileId: string) => {
    setDownloadingId(fileId);
    try {
      const file = await filesApi.getFileDownloadUrl(fileId, false);
      window.open(file.downloadUrl, '_blank', 'noopener,noreferrer');
    } catch {
      toast.error(t('financials.downloadReceiptError'));
    } finally {
      setDownloadingId(null);
    }
  };

  const downloadSettlement = async () => {
    setIsDownloadingSettlement(true);
    try {
      const blob = await financialsApi.downloadSettlementStatement(record.id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `Settlement_${record.id}.pdf`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      toast.error(t('financials.downloadSettlementError'));
    } finally {
      setIsDownloadingSettlement(false);
    }
  };

  const renderAllocation = (allocation: PaymentAllocationDto, direction: 'to' | 'from') => (
    <div key={allocation.id} className="py-2.5 first:pt-0 last:pb-0 border-b last:border-b-0 border-border/70 text-xs">
      <div className="flex items-center justify-between gap-3">
        <bdi dir="ltr" className="font-semibold font-mono text-foreground">
          {formatFinancialCurrency(allocation.allocatedAmount, record.currency, language)}
        </bdi>
        <StatusBadge
          label={allocationStatusValue(allocation.allocationStatus) === AllocationStatus.Active ? t('financials.allocationActive') : t('financials.allocationReversed')}
          variant={allocationStatusValue(allocation.allocationStatus) === AllocationStatus.Active ? 'success' : 'neutral'}
          size="sm"
        />
      </div>
      <div className="mt-1.5 text-muted-foreground">{direction === 'to' ? t('financials.allocatedToObligation') : t('financials.fundedByPayment')}</div>
      <div className="mt-1 flex items-center justify-between gap-3 text-muted-foreground">
        <span>{t('financials.allocationDate')}</span>
        <bdi dir="ltr" className="font-mono text-foreground">{formatFinancialDate(allocation.allocationDate)}</bdi>
      </div>
      {allocation.reversalReason && <p className="mt-1.5 text-danger">{allocation.reversalReason}</p>}
    </div>
  );

  const preview = view === 'tenant' ? tenantQuery
    : view === 'lease' ? leaseQuery
    : view === 'building' ? buildingQuery
    : view === 'apartment' ? apartmentQuery
    : null;

  return (
    <div className="fixed inset-0 z-50 overflow-hidden bg-black/40 backdrop-blur-xs flex justify-end animate-in fade-in duration-200" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <aside className="w-full sm:max-w-[540px] bg-card border-s border-border h-full shadow-2xl flex flex-col overflow-hidden" aria-label={view === 'record' ? t('financials.financialRecord') : titleByView[view]}>
        <header className="px-4 py-3 border-b border-border flex items-center justify-between gap-3">
          <div className="min-w-0 flex items-center gap-2.5">
            {view !== 'record' && (
              <button type="button" onClick={() => setView('record')} className="p-1.5 rounded-md hover:bg-secondary text-muted-foreground hover:text-foreground" aria-label={t('common.back')}>
                <ChevronRight className="w-4 h-4 rtl:rotate-0 rotate-180" />
              </button>
            )}
            <div className="min-w-0">
              <div className="flex items-center gap-2 min-w-0">
                <h2 className="text-base font-bold text-foreground truncate">
                  {view === 'record' ? t('financials.financialRecord') : titleByView[view]}
                </h2>
                {view === 'record' && <StatusBadge label={displayStatus.label} variant={displayStatus.variant} size="sm" />}
              </div>
            </div>
          </div>
          <button type="button" onClick={onClose} className="p-2 rounded-full hover:bg-secondary text-muted-foreground hover:text-foreground shrink-0" aria-label={t('common.close')}>
            <X className="w-5 h-5" />
          </button>
        </header>

        <div className="p-4 space-y-4 flex-1 overflow-y-auto overflow-x-hidden">
          {view !== 'record' ? (
            <EntityPreview view={view} query={preview!} language={language} t={t} />
          ) : (
            <>
              {isLoading && <div className="space-y-3"><Skeleton className="h-24 w-full" /><Skeleton className="h-36 w-full" /></div>}
              {isError && <ErrorState title={t('financials.detailLoadError')} onRetry={() => refetch()} />}

              {!isLoading && !isError && (
                <>
                  <section className={`grid ${obligation ? 'grid-cols-2 sm:grid-cols-4' : 'grid-cols-2'} gap-px overflow-hidden rounded-lg border border-border bg-border`}>
                    <Metric label={received ? t('financials.receivedAmount') : obligation ? t('financials.amountDue') : t('financials.adjustmentAmount')} value={formatFinancialCurrency(record.amountDue, record.currency, language)} />
                    {obligation && <Metric label={t('financials.amountPaid')} value={formatFinancialCurrency(record.amountPaid, record.currency, language)} className="text-success" />}
                    {obligation && <Metric label={t('financials.remaining')} value={remaining == null ? '—' : formatFinancialCurrency(remaining, record.currency, language)} className={remaining ? 'text-danger' : undefined} />}
                    <Metric label={t('financials.status')} value={displayStatus.label} mono={false} />
                  </section>

                  <div className="flex items-center justify-between gap-3 px-1 text-xs text-muted-foreground">
                    <span>{purposeConfig.label}</span>
                    <bdi dir="ltr" className="font-mono">{formatFinancialDate(obligation && record.dueDate ? record.dueDate : record.createdAt)}</bdi>
                  </div>

                  {summary?.isAvailable && obligation && (
                    <div className="flex justify-end">
                      <Button variant="outline" size="sm" onClick={downloadSettlement} disabled={isDownloadingSettlement} className="h-8 text-xs gap-1.5">
                        {isDownloadingSettlement ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Download className="w-3.5 h-3.5" />}
                        {t('financials.downloadSettlement')}
                      </Button>
                    </div>
                  )}

                  <section>
                    <SectionTitle>{t('financials.associatedEntities')}</SectionTitle>
                    <div className="mt-2 divide-y divide-border/70 rounded-lg border border-border px-2">
                      <EntityLink icon={User} label={t('financials.tenant')} value={record.tenantName} onClick={() => setView('tenant')} />
                      <EntityLink icon={FileText} label={t('financials.leaseContract')} value={record.contractNumber} onClick={() => setView('lease')} mono />
                      <EntityLink icon={Building2} label={t('financials.building')} value={record.buildingName} onClick={() => setView('building')} />
                      <EntityLink icon={Home} label={t('financials.apartment')} value={record.apartmentNumber ? t('financials.unitPrefix', { number: record.apartmentNumber }) : null} onClick={() => setView('apartment')} mono />
                    </div>
                  </section>

                  <section>
                    <SectionTitle>{t('financials.paymentData')}</SectionTitle>
                    <div className="mt-2 divide-y divide-border/70 rounded-lg border border-border px-3 text-xs">
                      <InfoRow label={t('financials.paymentMethod')} value={formatMethod(record.paymentMethod, t)} icon={CreditCard} />
                      {record.paymentReferenceNumber && <InfoRow label={t('financials.referenceNumber')} value={record.paymentReferenceNumber} icon={Wallet} mono />}
                      {record.latestSubmissionDate && <InfoRow label={t('financials.paymentDate')} value={formatFinancialDate(record.latestSubmissionDate)} icon={Calendar} mono />}
                      <InfoRow label={t('financials.recordedDate')} value={formatFinancialDate(record.createdAt)} icon={Calendar} mono />
                      {obligation && record.dueDate && <InfoRow label={t('financials.dueDate')} value={formatFinancialDate(record.dueDate)} icon={Calendar} mono />}
                      {obligation && record.billingPeriodStart && record.billingPeriodEnd && <InfoRow label={t('financials.billingPeriod')} value={`${formatFinancialDate(record.billingPeriodStart)} — ${formatFinancialDate(record.billingPeriodEnd)}`} icon={Calendar} mono />}
                      {details?.chequeDetails && (
                        <>
                          <InfoRow label={t('financials.chequeNumber')} value={details.chequeDetails.chequeNumber} icon={Landmark} mono />
                          <InfoRow label={t('financials.bankName')} value={details.chequeDetails.bankName} icon={Landmark} />
                        </>
                      )}
                    </div>
                    {record.notes && <p className="mt-2 px-3 py-2 rounded-md bg-secondary/40 text-xs whitespace-pre-wrap text-foreground">{record.notes}</p>}
                  </section>

                  {record.transactionReceipts?.length ? (
                    <section>
                      <SectionTitle>{t('financials.paymentMovements')}</SectionTitle>
                      <div className="mt-2 divide-y divide-border/70 rounded-lg border border-border px-3">
                        {record.transactionReceipts.map((transaction, index) => (
                          <div key={`${transaction.receiptId}-${index}`} className="py-2.5 flex items-center justify-between gap-3">
                            <div className="min-w-0 text-xs">
                              <div className="flex items-center gap-2 flex-wrap">
                                <Receipt className="w-3.5 h-3.5 text-primary" />
                                <bdi dir="ltr" className="font-semibold font-mono">{formatFinancialCurrency(transaction.amount, record.currency, language)}</bdi>
                                <bdi dir="ltr" className="text-muted-foreground font-mono">{transaction.receiptNumber}</bdi>
                              </div>
                              <bdi dir="ltr" className="block mt-1 text-[11px] text-muted-foreground font-mono">{formatFinancialDate(transaction.issuedAt)}</bdi>
                            </div>
                            {transaction.fileId && (
                              <Button variant="ghost" size="sm" onClick={() => downloadReceipt(transaction.fileId!)} disabled={downloadingId === transaction.fileId} className="h-8 text-xs gap-1.5 shrink-0">
                                {downloadingId === transaction.fileId ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Download className="w-3.5 h-3.5" />}
                                {t('financials.downloadReceipt')}
                              </Button>
                            )}
                          </div>
                        ))}
                      </div>
                    </section>
                  ) : null}

                  {(details?.incomingAllocations?.length || details?.outgoingAllocations?.length) ? (
                    <section>
                      <SectionTitle>{t('financials.allocations')}</SectionTitle>
                      <div className="mt-2 rounded-lg border border-border px-3">
                        {details?.incomingAllocations?.map((item) => renderAllocation(item, 'to'))}
                        {details?.outgoingAllocations?.map((item) => renderAllocation(item, 'from'))}
                      </div>
                    </section>
                  ) : null}

                  {isOutstanding && (
                    <div className="py-2.5 px-3 rounded-lg border border-warning/30 bg-warning-bg flex items-center justify-between gap-3">
                      <span className="text-xs text-warning font-medium flex items-center gap-2"><Bell className="w-4 h-4" />{t('financials.remindNotice')}</span>
                      <Button variant="outline" size="sm" disabled={remindMutation.isPending || remindMutation.isSuccess} onClick={() => remindMutation.mutate(record.id)} className="text-xs h-8 gap-1.5">
                        {remindMutation.isPending ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : remindMutation.isSuccess ? <CheckCircle2 className="w-3.5 h-3.5 text-success" /> : <Bell className="w-3.5 h-3.5" />}
                        {remindMutation.isSuccess ? t('financials.reminderSent') : t('financials.notifyTenant')}
                      </Button>
                    </div>
                  )}
                </>
              )}
            </>
          )}
        </div>
      </aside>
    </div>
  );
}

function EntityPreview({ view, query, language, t }: {
  view: Exclude<DrawerView, 'record'>;
  query: { data?: any; isLoading: boolean; isError: boolean; refetch: () => unknown };
  language: string;
  t: (key: string, values?: any) => string;
}) {
  if (query.isLoading) return <div className="space-y-2"><Skeleton className="h-16 w-full" /><Skeleton className="h-40 w-full" /></div>;
  if (query.isError || !query.data) return <ErrorState title={t('financials.entityLoadError')} onRetry={() => query.refetch()} />;
  const data = query.data;

  if (view === 'tenant') return (
    <PreviewSection title={data.name}>
      <PreviewRow label={t('financials.nationalId')} value={data.nationalId} mono />
      <PreviewRow label={t('financials.phone')} value={data.phone} mono />
      {data.email && <PreviewRow label={t('financials.email')} value={data.email} mono />}
      {data.occupation && <PreviewRow label={t('financials.occupation')} value={data.occupation} />}
      {data.employer && <PreviewRow label={t('financials.employer')} value={data.employer} />}
    </PreviewSection>
  );

  if (view === 'lease') return (
    <PreviewSection title={data.contractNumber} monoTitle>
      <PreviewRow label={t('financials.contractStatus')} value={contractStatusLabel(data.status, t)} />
      <PreviewRow label={t('financials.contractStart')} value={formatFinancialDate(data.startDate)} mono />
      <PreviewRow label={t('financials.contractEnd')} value={formatFinancialDate(data.endDate)} mono />
      <PreviewRow label={t('financials.monthlyRent')} value={formatFinancialCurrency(data.monthlyRentAmount, data.currency, language)} mono />
      <PreviewRow label={t('financials.securityDeposit')} value={formatFinancialCurrency(data.securityDepositAmount, data.currency, language)} mono />
      <PreviewRow label={t('financials.paymentFrequency')} value={paymentFrequencyLabel(data.paymentFrequency, t)} />
      <PreviewRow label={t('financials.paymentDueDay')} value={formatFinancialNumber(data.paymentDueDay)} mono />
      {data.externalRegistrationRef && <PreviewRow label={t('financials.externalRegistrationRef')} value={data.externalRegistrationRef} mono />}
    </PreviewSection>
  );

  if (view === 'building') {
    const address = data.address ? [data.address.district, data.address.area, data.address.streetName].filter(Boolean).join('، ') : null;
    return (
      <PreviewSection title={data.name}>
        <PreviewRow label={t('financials.buildingType')} value={buildingTypeLabel(data.buildingType, t)} />
        {data.internalCode && <PreviewRow label={t('financials.internalCode')} value={data.internalCode} mono />}
        <PreviewRow label={t('financials.totalFloors')} value={formatFinancialNumber(data.totalFloors)} mono />
        <PreviewRow label={t('financials.unitsCount')} value={formatFinancialNumber(data.totalApartmentsCount)} mono />
        {data.constructionYear && <PreviewRow label={t('financials.constructionYear')} value={formatFinancialNumber(data.constructionYear)} mono />}
        {address && <PreviewRow label={t('financials.address')} value={address} />}
      </PreviewSection>
    );
  }

  return (
    <PreviewSection title={t('financials.unitPrefix', { number: data.unitNumber })} monoTitle>
      <PreviewRow label={t('financials.occupancyStatus')} value={occupancyStatusLabel(data.occupancyStatus, t)} />
      <PreviewRow label={t('financials.ownershipStatus')} value={ownershipStatusLabel(data.ownershipStatus, t)} />
      <PreviewRow label={t('financials.area')} value={`${formatFinancialNumber(data.areaSqm)} ${t('financials.squareMeters')}`} mono />
      <PreviewRow label={t('financials.bedrooms')} value={formatFinancialNumber(data.bedrooms)} mono />
      <PreviewRow label={t('financials.bathrooms')} value={formatFinancialNumber(data.bathrooms)} mono />
      {data.baseRentAmount != null && <PreviewRow label={t('financials.baseRent')} value={formatFinancialCurrency(data.baseRentAmount, data.baseRentCurrency, language)} mono />}
    </PreviewSection>
  );
}

function PreviewSection({ title, children, monoTitle = false }: { title: string; children: React.ReactNode; monoTitle?: boolean }) {
  return <section className="space-y-3"><h3 className={`text-base font-semibold text-foreground ${monoTitle ? 'font-mono' : ''}`}>{title}</h3><div className="divide-y divide-border/70 rounded-lg border border-border px-3 text-xs">{children}</div></section>;
}

function PreviewRow({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return <div className="py-2.5 flex items-center justify-between gap-4"><span className="text-muted-foreground">{label}</span><bdi dir={mono ? 'ltr' : undefined} className={`font-medium text-foreground text-end break-words ${mono ? 'font-mono' : ''}`}>{value || '—'}</bdi></div>;
}

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h3 className="text-xs font-semibold text-foreground">{children}</h3>;
}

function Metric({ label, value, className = '', mono = true }: { label: string; value: string; className?: string; mono?: boolean }) {
  return <div className="bg-card px-2.5 py-3 text-center min-w-0"><span className="text-[11px] text-muted-foreground block truncate">{label}</span><bdi dir={mono ? 'ltr' : undefined} className={`mt-0.5 block text-sm font-bold text-foreground truncate ${mono ? 'font-mono' : ''} ${className}`}>{value}</bdi></div>;
}

function EntityLink({ icon: Icon, label, value, onClick, mono = false }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string | null; onClick: () => void; mono?: boolean }) {
  return <button type="button" onClick={onClick} className="group w-full min-w-0 flex items-center gap-2.5 px-1 py-2.5 hover:text-primary text-start transition-colors"><Icon className="w-4 h-4 text-muted-foreground group-hover:text-primary shrink-0" /><span className="text-[11px] text-muted-foreground w-20 shrink-0">{label}</span><bdi dir={mono ? 'ltr' : undefined} title={value || undefined} className={`min-w-0 flex-1 truncate text-xs font-medium text-foreground ${mono ? 'font-mono' : ''}`}>{value || '—'}</bdi><ChevronRight className="w-3.5 h-3.5 text-muted-foreground rtl:rotate-180 shrink-0" /></button>;
}

function InfoRow({ label, value, icon: Icon, mono = false }: { label: string; value: string; icon: React.ComponentType<{ className?: string }>; mono?: boolean }) {
  return <div className="py-2.5 flex items-center justify-between gap-4"><span className="text-muted-foreground flex items-center gap-2 shrink-0"><Icon className="w-3.5 h-3.5" />{label}</span><bdi dir={mono ? 'ltr' : undefined} className={`font-medium text-foreground text-end break-words ${mono ? 'font-mono' : ''}`}>{value || '—'}</bdi></div>;
}

function getPurposeConfig(purpose: RentPaymentDto['paymentPurpose'], t: (key: string) => string): { label: string; variant: StatusVariant } {
  switch (paymentPurposeValue(purpose)) {
    case PaymentPurpose.ScheduledInstallment: return { label: t('financials.purposeObligation'), variant: 'warning' };
    case PaymentPurpose.UnallocatedReceipt: return { label: t('financials.purposeReceivedPayment'), variant: 'success' };
    case PaymentPurpose.Adjustment: return { label: t('financials.purposeAdjustment'), variant: 'info' };
    default: return { label: String(purpose), variant: 'neutral' };
  }
}

function getStatusConfig(status: RentPaymentDto['dueDateStatus'], t: (key: string) => string): { label: string; variant: StatusVariant } {
  switch (dueDateStatusValue(status)) {
    case DueDateStatus.Paid: return { label: t('financials.statusPaid'), variant: 'success' };
    case DueDateStatus.PartiallyPaid: return { label: t('financials.statusPartiallyPaid'), variant: 'warning' };
    case DueDateStatus.Late: return { label: t('financials.statusLate'), variant: 'warning' };
    case DueDateStatus.OverdueUnpaid: return { label: t('financials.statusOverdue'), variant: 'danger' };
    case DueDateStatus.PendingVerification: return { label: t('financials.statusPendingVerification'), variant: 'info' };
    case DueDateStatus.Cancelled: return { label: t('financials.statusCancelled'), variant: 'neutral' };
    default: return { label: t('financials.statusPending'), variant: 'neutral' };
  }
}

function formatMethod(method: RentPaymentDto['paymentMethod'], t: (key: string) => string): string {
  switch (paymentMethodValue(method)) {
    case PaymentMethod.Cash: return t('financials.paymentMethodCash');
    case PaymentMethod.BankTransfer: return t('financials.paymentMethodBankTransfer');
    case PaymentMethod.Cheque: return t('financials.paymentMethodCheque');
    case PaymentMethod.Efawateercom: return t('financials.paymentMethodEfawateercom');
    case PaymentMethod.CliQ: return t('financials.paymentMethodCliq');
    default: return '—';
  }
}

function contractStatusLabel(value: ContractStatus, t: (key: string) => string): string {
  const labels: Record<number, string> = {
    [ContractStatus.Draft]: t('financials.contractStatusDraft'), [ContractStatus.PendingSignature]: t('financials.contractStatusPendingSignature'),
    [ContractStatus.Active]: t('financials.contractStatusActive'), [ContractStatus.Expired]: t('financials.contractStatusExpired'),
    [ContractStatus.Renewed]: t('financials.contractStatusRenewed'), [ContractStatus.Terminated]: t('financials.contractStatusTerminated'),
    [ContractStatus.Cancelled]: t('financials.contractStatusCancelled'), [ContractStatus.Superseded]: t('financials.contractStatusSuperseded'),
  };
  return labels[value] || String(value);
}

function paymentFrequencyLabel(value: PaymentFrequency, t: (key: string) => string): string {
  const labels: Record<number, string> = {
    [PaymentFrequency.Monthly]: t('financials.frequencyMonthly'), [PaymentFrequency.Quarterly]: t('financials.frequencyQuarterly'),
    [PaymentFrequency.SemiAnnual]: t('financials.frequencySemiAnnual'), [PaymentFrequency.Annual]: t('financials.frequencyAnnual'),
  };
  return labels[value] || String(value);
}

function buildingTypeLabel(value: BuildingType, t: (key: string) => string): string {
  const labels: Record<number, string> = {
    [BuildingType.Residential]: t('financials.buildingResidential'), [BuildingType.Commercial]: t('financials.buildingCommercial'),
    [BuildingType.MixedUse]: t('financials.buildingMixedUse'),
  };
  return labels[value] || String(value);
}

function occupancyStatusLabel(value: OccupancyStatus, t: (key: string) => string): string {
  const labels: Record<number, string> = {
    [OccupancyStatus.Vacant]: t('financials.occupancyVacant'), [OccupancyStatus.Occupied]: t('financials.occupancyOccupied'),
    [OccupancyStatus.UnderMaintenance]: t('financials.occupancyMaintenance'), [OccupancyStatus.Listed]: t('financials.occupancyListed'),
  };
  return labels[value] || String(value);
}

function ownershipStatusLabel(value: OwnershipStatus, t: (key: string) => string): string {
  return value === OwnershipStatus.CompanyOwned ? t('financials.ownershipCompany')
    : value === OwnershipStatus.ThirdPartyOwned ? t('financials.ownershipThirdParty') : String(value);
}
