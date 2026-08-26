import React from 'react';
import {
  AlertCircle, CalendarClock, ChevronLeft, ChevronRight, Eye, FileCheck2,
  ReceiptText, SlidersHorizontal,
} from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { EmptyState, Skeleton } from '@/shared/components/ui/Feedback';
import { StatusBadge, type StatusVariant } from '@/shared/components/ui/StatusBadge';
import { useTranslation } from '@/shared/i18n';
import { DueDateStatus, PaymentMethod, PaymentPurpose, type RentPaymentDto } from '../types/financials.types';
import {
  dueDateStatusValue, formatFinancialCurrency, formatFinancialDate, formatFinancialNumber,
  isScheduledInstallment, paymentMethodValue, paymentPurposeValue,
} from '../utils/financialRecord';

interface RentCollectionTableProps {
  payments: RentPaymentDto[];
  isLoading: boolean;
  isError: boolean;
  onSelectPayment: (payment: RentPaymentDto) => void;
  onNextPage?: () => void;
  onPrevPage?: () => void;
  onRetry?: () => void;
  hasNextPage?: boolean;
  hasPrevPage?: boolean;
  currentPageIndex?: number;
  onResetFilters?: () => void;
}

export function RentCollectionTable({
  payments, isLoading, isError, onSelectPayment, onNextPage, onPrevPage, onRetry,
  hasNextPage = false, hasPrevPage = false, currentPageIndex = 1, onResetFilters,
}: RentCollectionTableProps) {
  const { t, language } = useTranslation();

  if (isLoading) {
    return <div className="bg-card rounded-lg border border-border overflow-hidden p-4 space-y-3">{[1, 2, 3, 4, 5].map((item) => <Skeleton key={item} className="h-11 w-full" />)}</div>;
  }

  if (isError) {
    return (
      <div className="bg-card rounded-lg border border-danger/30 p-8 text-center space-y-3">
        <AlertCircle className="w-8 h-8 text-danger mx-auto" />
        <p className="text-sm font-semibold text-danger">{t('financials.loadError')}</p>
        {onRetry && <Button variant="outline" size="sm" onClick={onRetry}>{t('common.retry')}</Button>}
      </div>
    );
  }

  if (!payments.length) {
    return <EmptyState title={t('financials.emptyTitle')} action={onResetFilters ? <Button variant="outline" size="sm" onClick={onResetFilters}>{t('common.clear')}</Button> : undefined} />;
  }

  const obligations = payments.filter((item) => paymentPurposeValue(item.paymentPurpose) === PaymentPurpose.ScheduledInstallment);
  const receivedPayments = payments.filter((item) => paymentPurposeValue(item.paymentPurpose) === PaymentPurpose.UnallocatedReceipt);
  const adjustments = payments.filter((item) => paymentPurposeValue(item.paymentPurpose) === PaymentPurpose.Adjustment);

  return (
    <div className="space-y-5">
      {obligations.length > 0 && (
        <RecordSection
          title={t('financials.rentObligationsSection')}
          icon={CalendarClock}
          records={obligations}
          onSelect={onSelectPayment}
          language={language}
          t={t}
        />
      )}
      {receivedPayments.length > 0 && (
        <RecordSection
          title={t('financials.paymentsSection')}
          icon={ReceiptText}
          records={receivedPayments}
          onSelect={onSelectPayment}
          language={language}
          t={t}
        />
      )}
      {adjustments.length > 0 && (
        <RecordSection
          title={t('financials.adjustmentsSection')}
          icon={SlidersHorizontal}
          records={adjustments}
          onSelect={onSelectPayment}
          language={language}
          t={t}
        />
      )}

      <div className="flex items-center justify-between px-1 text-xs text-muted-foreground">
        <span>{t('financials.showingPage', { page: formatFinancialNumber(currentPageIndex) })}</span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={onPrevPage} disabled={!hasPrevPage} className="gap-1 h-8 px-3 text-xs">
            <ChevronRight className="w-3.5 h-3.5 rtl:rotate-0 rotate-180" /><span>{t('table.previous')}</span>
          </Button>
          <Button variant="outline" size="sm" onClick={onNextPage} disabled={!hasNextPage} className="gap-1 h-8 px-3 text-xs">
            <span>{t('table.next')}</span><ChevronLeft className="w-3.5 h-3.5 rtl:rotate-0 rotate-180" />
          </Button>
        </div>
      </div>
    </div>
  );
}

function RecordSection({ title, icon: Icon, records, onSelect, language, t }: {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  records: RentPaymentDto[];
  onSelect: (record: RentPaymentDto) => void;
  language: string;
  t: (key: string, params?: Record<string, unknown>) => string;
}) {
  return (
    <section className="space-y-2">
      <div className="flex items-center gap-2 px-1">
        <Icon className="w-4 h-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">{title}</h2>
        <span className="text-[11px] font-mono text-muted-foreground" dir="ltr">({formatFinancialNumber(records.length)})</span>
      </div>

      <div className="hidden md:block bg-card rounded-lg border border-border overflow-x-auto shadow-2xs">
        <table className="w-full min-w-[900px] text-xs text-start border-collapse">
          <thead>
            <tr className="border-b border-border bg-secondary/40 text-muted-foreground font-semibold">
              <th className="py-2.5 px-4 text-start">{t('financials.tenant')}</th>
              <th className="py-2.5 px-3 text-start">{t('financials.propertyAndUnit')}</th>
              <th className="py-2.5 px-3 text-start">{t('financials.recordType')}</th>
              <th className="py-2.5 px-3 text-start">{t('financials.amount')}</th>
              <th className="py-2.5 px-3 text-start">{t('financials.remaining')}</th>
              <th className="py-2.5 px-3 text-start">{t('financials.relevantDate')}</th>
              <th className="py-2.5 px-3 text-center">{t('financials.status')}</th>
              <th className="py-2.5 px-4 text-end">{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {records.map((record) => <DesktopRow key={record.id} record={record} onSelect={onSelect} language={language} t={t} />)}
          </tbody>
        </table>
      </div>

      <div className="md:hidden divide-y divide-border rounded-lg border border-border bg-card overflow-hidden">
        {records.map((record) => <MobileRecord key={record.id} record={record} onSelect={onSelect} language={language} t={t} />)}
      </div>
    </section>
  );
}

function DesktopRow({ record, onSelect, language, t }: RecordRowProps) {
  const obligation = isScheduledInstallment(record.paymentPurpose);
  const purpose = getPurposeConfig(record.paymentPurpose, t);
  const method = formatMethod(record.paymentMethod, t);
  const remaining = obligation ? record.settlementSummary?.remaining : null;
  const receiptAvailable = Boolean(record.receiptFileId || record.transactionReceipts?.some((item) => item.fileId));

  return (
    <tr className="hover:bg-secondary/30 transition-colors cursor-pointer" onClick={() => onSelect(record)}>
      <td className="py-3 px-4 font-medium text-foreground">
        <span>{record.tenantName || '—'}</span>
        {record.contractNumber && <bdi dir="ltr" className="block text-[10px] text-muted-foreground mt-0.5 font-mono">{record.contractNumber}</bdi>}
      </td>
      <td className="py-3 px-3 text-muted-foreground">
        <span className="block text-foreground">{record.buildingName || '—'}</span>
        {record.apartmentNumber && <bdi dir="ltr" className="block text-[10px] mt-0.5 font-mono">{t('financials.unitPrefix', { number: record.apartmentNumber })}</bdi>}
      </td>
      <td className="py-3 px-3">
        <StatusBadge label={purpose.label} variant={purpose.variant} size="sm" />
        {method && <span className="block text-[10px] text-muted-foreground mt-1">{method}</span>}
      </td>
      <td className="py-3 px-3 font-semibold font-mono text-foreground"><bdi dir="ltr">{formatFinancialCurrency(record.amountDue, record.currency, language)}</bdi></td>
      <td className="py-3 px-3 font-semibold font-mono">{remaining == null ? '—' : <bdi dir="ltr">{formatFinancialCurrency(remaining, record.currency, language)}</bdi>}</td>
      <td className="py-3 px-3 text-muted-foreground font-mono" dir="ltr">{formatFinancialDate(obligation && record.dueDate ? record.dueDate : record.createdAt)}</td>
      <td className="py-3 px-3 text-center">
        {obligation ? <StatusBadge {...getStatusConfig(record.dueDateStatus, t)} size="sm" />
          : receiptAvailable ? <span className="inline-flex items-center gap-1 text-[10px] text-success font-medium"><FileCheck2 className="w-3.5 h-3.5" />{t('financials.receiptAvailable')}</span>
          : '—'}
      </td>
      <td className="py-3 px-4 text-end" onClick={(event) => event.stopPropagation()}>
        <Button variant="ghost" size="sm" aria-label={t('common.viewDetails')} onClick={() => onSelect(record)} className="h-8 gap-1.5 text-xs">
          <Eye className="w-3.5 h-3.5" />{t('financials.openRecord')}
        </Button>
      </td>
    </tr>
  );
}

function MobileRecord({ record, onSelect, language, t }: RecordRowProps) {
  const obligation = isScheduledInstallment(record.paymentPurpose);
  const purpose = getPurposeConfig(record.paymentPurpose, t);
  const remaining = obligation ? record.settlementSummary?.remaining : null;
  return (
    <button type="button" onClick={() => onSelect(record)} className="w-full p-3 text-start hover:bg-secondary/30">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <span className="block text-sm font-semibold text-foreground truncate">{record.tenantName || '—'}</span>
          <span className="block mt-0.5 text-[11px] text-muted-foreground truncate">{[record.buildingName, record.apartmentNumber ? t('financials.unitPrefix', { number: record.apartmentNumber }) : null].filter(Boolean).join(' · ')}</span>
        </div>
        <StatusBadge label={purpose.label} variant={purpose.variant} size="sm" />
      </div>
      <div className="mt-3 grid grid-cols-2 gap-3">
        <div><span className="block text-[10px] text-muted-foreground">{t('financials.amount')}</span><bdi dir="ltr" className="block mt-0.5 font-mono font-semibold text-foreground">{formatFinancialCurrency(record.amountDue, record.currency, language)}</bdi></div>
        <div><span className="block text-[10px] text-muted-foreground">{obligation ? t('financials.remaining') : t('financials.relevantDate')}</span><bdi dir="ltr" className="block mt-0.5 font-mono font-semibold text-foreground">{obligation ? remaining == null ? '—' : formatFinancialCurrency(remaining, record.currency, language) : formatFinancialDate(record.createdAt)}</bdi></div>
      </div>
      <div className="mt-2.5 flex items-center justify-between gap-3">
        <bdi dir="ltr" className="text-[11px] font-mono text-muted-foreground">{obligation ? formatFinancialDate(record.dueDate) : record.paymentReferenceNumber || '—'}</bdi>
        {obligation && <StatusBadge {...getStatusConfig(record.dueDateStatus, t)} size="sm" />}
      </div>
    </button>
  );
}

interface RecordRowProps {
  record: RentPaymentDto;
  onSelect: (record: RentPaymentDto) => void;
  language: string;
  t: (key: string, params?: Record<string, unknown>) => string;
}

function getStatusConfig(status: RentPaymentDto['dueDateStatus'], t: RecordRowProps['t']): { label: string; variant: StatusVariant } {
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

function getPurposeConfig(purpose: RentPaymentDto['paymentPurpose'], t: RecordRowProps['t']): { label: string; variant: StatusVariant } {
  switch (paymentPurposeValue(purpose)) {
    case PaymentPurpose.ScheduledInstallment: return { label: t('financials.purposeObligation'), variant: 'warning' };
    case PaymentPurpose.UnallocatedReceipt: return { label: t('financials.purposeReceivedPayment'), variant: 'success' };
    case PaymentPurpose.Adjustment: return { label: t('financials.purposeAdjustment'), variant: 'info' };
    default: return { label: String(purpose), variant: 'neutral' };
  }
}

function formatMethod(method: RentPaymentDto['paymentMethod'], t: RecordRowProps['t']): string | null {
  switch (paymentMethodValue(method)) {
    case PaymentMethod.Cash: return t('financials.paymentMethodCash');
    case PaymentMethod.BankTransfer: return t('financials.paymentMethodBankTransfer');
    case PaymentMethod.Cheque: return t('financials.paymentMethodCheque');
    case PaymentMethod.Efawateercom: return t('financials.paymentMethodEfawateercom');
    case PaymentMethod.CliQ: return t('financials.paymentMethodCliq');
    default: return null;
  }
}
