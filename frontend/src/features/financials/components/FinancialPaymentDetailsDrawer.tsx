import React from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from '@/shared/i18n';
import { StatusBadge, type StatusVariant } from '@/shared/components/ui/StatusBadge';
import { usePaymentDetails, usePaymentReceipt, useRemindRentPayment } from '../hooks/useRentPayments';
import { DueDateStatus, PaymentMethod, type RentPaymentDto } from '../types/financials.types';
import { ROUTES } from '@/config/routes';
import {
  X,
  Building2,
  Home,
  User,
  FileText,
  Calendar,
  Wallet,
  Receipt,
  CreditCard,
  Bell,
  ExternalLink,
  CheckCircle2,
  Loader2,
} from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { Skeleton } from '@/shared/components/ui/Feedback';

interface FinancialPaymentDetailsDrawerProps {
  payment: RentPaymentDto | null;
  onClose: () => void;
}

export function FinancialPaymentDetailsDrawer({ payment, onClose }: FinancialPaymentDetailsDrawerProps) {
  const navigate = useNavigate();
  const { t, language, formatCurrency, formatDate } = useTranslation();

  const { data: details, isLoading } = usePaymentDetails(payment?.id || null);
  const { data: receipt } = usePaymentReceipt(payment?.id || null);
  const remindMutation = useRemindRentPayment();

  if (!payment) return null;

  const remainingAmount = Math.max(0, payment.amountDue - payment.amountPaid);
  const isOutstanding = Number(payment.dueDateStatus) !== DueDateStatus.Paid &&
                        Number(payment.dueDateStatus) !== DueDateStatus.Cancelled &&
                        remainingAmount > 0;

  const getStatusBadgeConfig = (status: DueDateStatus | number | string): { label: string; variant: StatusVariant } => {
    const num = Number(status);
    switch (num) {
      case DueDateStatus.Paid:
        return { label: t('financials.statusPaid'), variant: 'success' };
      case DueDateStatus.PartiallyPaid:
        return { label: t('financials.statusPartiallyPaid'), variant: 'warning' };
      case DueDateStatus.Late:
        return { label: t('financials.statusLate'), variant: 'warning' };
      case DueDateStatus.OverdueUnpaid:
        return { label: t('financials.statusOverdue'), variant: 'danger' };
      case DueDateStatus.PendingVerification:
        return { label: t('financials.statusPendingVerification'), variant: 'info' };
      case DueDateStatus.Cancelled:
        return { label: t('financials.statusCancelled'), variant: 'neutral' };
      case DueDateStatus.Pending:
      default:
        return { label: t('financials.statusPending'), variant: 'neutral' };
    }
  };

  const statusConfig = getStatusBadgeConfig(payment.dueDateStatus);

  const formatMethod = (method: PaymentMethod | number | string | null | undefined): string => {
    if (method === null || method === undefined) return '—';
    const num = Number(method);
    switch (num) {
      case PaymentMethod.Cash:
        return t('financials.paymentMethodCash');
      case PaymentMethod.BankTransfer:
        return t('financials.paymentMethodBankTransfer');
      case PaymentMethod.Cheque:
        return t('financials.paymentMethodCheque');
      case PaymentMethod.Efawateercom:
        return t('financials.paymentMethodEfawateercom');
      case PaymentMethod.CliQ:
        return t('financials.paymentMethodCliq');
      default:
        return String(method);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-hidden bg-black/40 backdrop-blur-xs flex justify-end animate-in fade-in duration-200">
      <div className="w-full max-w-lg bg-card border-s border-border h-full shadow-2xl flex flex-col justify-between overflow-y-auto">
        
        {/* Drawer Header */}
        <div className="p-6 border-b border-border flex items-center justify-between">
          <div>
            <h2 className="text-lg font-bold text-foreground">
              {t('financials.paymentDetails')}
            </h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              {payment.contractNumber ? t('financials.contractPrefix', { number: payment.contractNumber }) : `ID: ${payment.id.slice(0, 8)}`}
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-full hover:bg-secondary text-muted-foreground hover:text-foreground cursor-pointer transition-colors"
            aria-label={t('common.close')}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Drawer Body */}
        <div className="p-6 space-y-6 flex-1 overflow-y-auto">
          
          {/* Status & Amount Hero Banner */}
          <div className="p-4 rounded-lg bg-secondary/50 border border-border space-y-3">
            <div className="flex items-center justify-between">
              <StatusBadge label={statusConfig.label} variant={statusConfig.variant} size="md" />
              <span className="text-xs text-muted-foreground font-mono">
                {payment.dueDate ? `${t('financials.dueDate')}: ${formatDate(payment.dueDate, language)}` : '—'}
              </span>
            </div>

            <div className="grid grid-cols-3 gap-2 pt-2 border-t border-border/60 text-center">
              <div>
                <span className="text-[11px] text-muted-foreground block">{t('financials.amountDue')}</span>
                <span className="text-sm font-bold font-mono text-foreground">
                  {formatCurrency(payment.amountDue, { currency: payment.currency })}
                </span>
              </div>
              <div>
                <span className="text-[11px] text-muted-foreground block">{t('financials.amountPaid')}</span>
                <span className="text-sm font-bold font-mono text-success">
                  {formatCurrency(payment.amountPaid, { currency: payment.currency })}
                </span>
              </div>
              <div>
                <span className="text-[11px] text-muted-foreground block">{t('financials.remaining')}</span>
                <span className={`text-sm font-bold font-mono ${remainingAmount > 0 ? 'text-danger' : 'text-muted-foreground'}`}>
                  {formatCurrency(remainingAmount, { currency: payment.currency })}
                </span>
              </div>
            </div>
          </div>

          {/* Tenant & Property Association */}
          <div className="space-y-3">
            <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              {t('financials.associatedEntities')}
            </h3>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Tenant Link */}
              <div
                onClick={() => payment.tenantId && navigate(ROUTES.tenants.details(payment.tenantId))}
                className="p-3 rounded-lg border border-border bg-card hover:bg-secondary/40 transition-colors cursor-pointer flex items-center justify-between"
              >
                <div className="flex items-center gap-2.5">
                  <User className="w-4 h-4 text-primary shrink-0" />
                  <div>
                    <span className="text-[10px] text-muted-foreground block">{t('financials.tenant')}</span>
                    <span className="text-xs font-medium text-foreground">{payment.tenantName || '—'}</span>
                  </div>
                </div>
                <ExternalLink className="w-3.5 h-3.5 text-muted-foreground" />
              </div>

              {/* Building Link */}
              <div
                onClick={() => payment.buildingId && navigate(ROUTES.buildings.details(payment.buildingId))}
                className="p-3 rounded-lg border border-border bg-card hover:bg-secondary/40 transition-colors cursor-pointer flex items-center justify-between"
              >
                <div className="flex items-center gap-2.5">
                  <Building2 className="w-4 h-4 text-primary shrink-0" />
                  <div>
                    <span className="text-[10px] text-muted-foreground block">{t('financials.building')}</span>
                    <span className="text-xs font-medium text-foreground">{payment.buildingName || '—'}</span>
                  </div>
                </div>
                <ExternalLink className="w-3.5 h-3.5 text-muted-foreground" />
              </div>

              {/* Apartment Link */}
              <div
                onClick={() => payment.apartmentId && navigate(ROUTES.apartments.details(payment.apartmentId))}
                className="p-3 rounded-lg border border-border bg-card hover:bg-secondary/40 transition-colors cursor-pointer flex items-center justify-between"
              >
                <div className="flex items-center gap-2.5">
                  <Home className="w-4 h-4 text-primary shrink-0" />
                  <div>
                    <span className="text-[10px] text-muted-foreground block">{t('financials.apartment')}</span>
                    <span className="text-xs font-medium text-foreground">
                      {payment.apartmentNumber ? t('financials.unitPrefix', { number: payment.apartmentNumber }) : '—'}
                    </span>
                  </div>
                </div>
                <ExternalLink className="w-3.5 h-3.5 text-muted-foreground" />
              </div>

              {/* Lease Contract Link */}
              <div
                onClick={() => payment.leaseContractId && navigate(ROUTES.leases.details(payment.leaseContractId))}
                className="p-3 rounded-lg border border-border bg-card hover:bg-secondary/40 transition-colors cursor-pointer flex items-center justify-between"
              >
                <div className="flex items-center gap-2.5">
                  <FileText className="w-4 h-4 text-primary shrink-0" />
                  <div>
                    <span className="text-[10px] text-muted-foreground block">{t('financials.leaseContract')}</span>
                    <span className="text-xs font-medium text-foreground">{payment.contractNumber || '—'}</span>
                  </div>
                </div>
                <ExternalLink className="w-3.5 h-3.5 text-muted-foreground" />
              </div>
            </div>
          </div>

          {/* Payment Details Breakdown */}
          <div className="space-y-3">
            <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              {t('financials.scheduleInfo')}
            </h3>

            <div className="rounded-lg border border-border bg-card divide-y divide-border text-xs">
              <div className="p-3 flex justify-between">
                <span className="text-muted-foreground">{t('financials.billingPeriod')}</span>
                <span className="font-medium text-foreground">
                  {payment.billingPeriodStart && payment.billingPeriodEnd
                    ? `${formatDate(payment.billingPeriodStart, language)} — ${formatDate(payment.billingPeriodEnd, language)}`
                    : '—'}
                </span>
              </div>

              <div className="p-3 flex justify-between">
                <span className="text-muted-foreground">{t('financials.paymentMethod')}</span>
                <span className="font-medium text-foreground">{formatMethod(payment.paymentMethod)}</span>
              </div>

              {payment.paymentReferenceNumber && (
                <div className="p-3 flex justify-between">
                  <span className="text-muted-foreground">{t('financials.referenceNumber')}</span>
                  <span className="font-mono text-foreground">{payment.paymentReferenceNumber}</span>
                </div>
              )}

              {payment.receiptNumber && (
                <div className="p-3 flex justify-between items-center">
                  <span className="text-muted-foreground">{t('financials.receiptNumber')}</span>
                  <span className="inline-flex items-center gap-1 font-mono text-primary font-semibold">
                    <Receipt className="w-3.5 h-3.5" />
                    <span>{payment.receiptNumber}</span>
                  </span>
                </div>
              )}

              {payment.notes && (
                <div className="p-3 flex flex-col gap-1">
                  <span className="text-muted-foreground">{t('financials.notes')}</span>
                  <p className="text-foreground">{payment.notes}</p>
                </div>
              )}
            </div>
          </div>

          {/* Notify Tenant Slot */}
          {isOutstanding && (
            <div className="p-3.5 rounded-lg border border-warning/30 bg-warning-bg flex items-center justify-between gap-3">
              <div className="flex items-center gap-2 min-w-0">
                <Bell className="w-4 h-4 text-warning shrink-0" />
                <span className="text-xs text-warning font-medium truncate">
                  {t('financials.remindNotice')}
                </span>
              </div>
              <Button
                variant="outline"
                size="sm"
                disabled={remindMutation.isPending || remindMutation.isSuccess}
                onClick={() => remindMutation.mutate(payment.id)}
                className="text-xs shrink-0 h-8 gap-1.5 border-warning/40 text-foreground hover:bg-warning/10"
              >
                {remindMutation.isPending ? (
                  <>
                    <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    <span>{t('financials.sendingReminder')}</span>
                  </>
                ) : remindMutation.isSuccess ? (
                  <>
                    <CheckCircle2 className="w-3.5 h-3.5 text-success" />
                    <span>{t('financials.reminderSent')}</span>
                  </>
                ) : (
                  <>
                    <Bell className="w-3.5 h-3.5" />
                    <span>{t('financials.notifyTenant')}</span>
                  </>
                )}
              </Button>
            </div>
          )}
        </div>

        {/* Drawer Footer */}
        <div className="p-4 border-t border-border bg-card flex justify-end">
          <Button variant="secondary" size="sm" onClick={onClose}>
            {t('common.close')}
          </Button>
        </div>

      </div>
    </div>
  );
}
