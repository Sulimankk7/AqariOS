import React from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from '@/shared/i18n';
import { StatusBadge, type StatusVariant } from '@/shared/components/ui/StatusBadge';
import { DueDateStatus, type RentPaymentDto } from '../types/financials.types';
import { ROUTES } from '@/config/routes';
import { Eye, ChevronRight, ChevronLeft, Receipt, AlertCircle, FileText } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { Skeleton, EmptyState } from '@/shared/components/ui/Feedback';

interface RentCollectionTableProps {
  payments: RentPaymentDto[];
  isLoading: boolean;
  isError: boolean;
  onSelectPayment: (payment: RentPaymentDto) => void;
  onNextPage?: () => void;
  onPrevPage?: () => void;
  hasNextPage?: boolean;
  hasPrevPage?: boolean;
  currentPageIndex?: number;
  onResetFilters?: () => void;
}

export function RentCollectionTable({
  payments,
  isLoading,
  isError,
  onSelectPayment,
  onNextPage,
  onPrevPage,
  hasNextPage = false,
  hasPrevPage = false,
  currentPageIndex = 1,
  onResetFilters,
}: RentCollectionTableProps) {
  const navigate = useNavigate();
  const { t, language, formatCurrency, formatDate } = useTranslation();

  const getStatusConfig = (status: DueDateStatus | number | string): { label: string; variant: StatusVariant } => {
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

  if (isLoading) {
    return (
      <div className="bg-card rounded-lg border border-border overflow-hidden">
        <div className="p-4 space-y-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="flex items-center justify-between gap-4 py-2 border-b border-border/50 last:border-0">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-6 w-20 rounded-full" />
              <Skeleton className="h-8 w-8 rounded-md" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="bg-card rounded-lg border border-danger/30 p-8 text-center space-y-3">
        <AlertCircle className="w-8 h-8 text-danger mx-auto" />
        <p className="text-sm font-semibold text-danger">
          {t('financials.loadError')}
        </p>
      </div>
    );
  }

  if (!payments || payments.length === 0) {
    return (
      <div className="bg-card rounded-lg border border-border p-12 text-center">
        <EmptyState
          title={t('financials.emptyTitle')}
          description={t('financials.emptyDescription')}
          action={
            onResetFilters && (
              <Button variant="outline" size="sm" onClick={onResetFilters} className="mt-4">
                {t('common.clear')}
              </Button>
            )
          }
        />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Desktop & Tablet Table */}
      <div className="bg-card rounded-lg border border-border overflow-x-auto shadow-2xs">
        <table className="w-full text-xs text-start border-collapse">
          <thead>
            <tr className="border-b border-border bg-secondary/50 text-muted-foreground font-semibold">
              <th className="py-3 px-4 text-start">{t('financials.tenant')}</th>
              <th className="py-3 px-3 text-start">{t('financials.building')}</th>
              <th className="py-3 px-3 text-start">{t('financials.apartment')}</th>
              <th className="py-3 px-3 text-start">{t('financials.amountDue')}</th>
              <th className="py-3 px-3 text-start">{t('financials.amountPaid')}</th>
              <th className="py-3 px-3 text-start">{t('financials.remaining')}</th>
              <th className="py-3 px-3 text-start">{t('financials.dueDate')}</th>
              <th className="py-3 px-3 text-center">{t('financials.status')}</th>
              <th className="py-3 px-4 text-end">{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {payments.map((p) => {
              const statusCfg = getStatusConfig(p.dueDateStatus);
              const remaining = Math.max(0, p.amountDue - p.amountPaid);

              return (
                <tr
                  key={p.id}
                  className="hover:bg-secondary/30 transition-colors group cursor-pointer"
                  onClick={() => onSelectPayment(p)}
                >
                  {/* Tenant */}
                  <td className="py-3.5 px-4 font-medium text-foreground">
                    <span
                      onClick={(e) => {
                        if (p.tenantId) {
                          e.stopPropagation();
                          navigate(ROUTES.tenants.details(p.tenantId));
                        }
                      }}
                      className="hover:text-primary hover:underline"
                    >
                      {p.tenantName || '—'}
                    </span>
                    {p.receiptNumber && (
                      <span className="flex items-center gap-1 text-[10px] text-muted-foreground mt-0.5 font-mono">
                        <Receipt className="w-3 h-3 text-primary" />
                        <span>#{p.receiptNumber}</span>
                      </span>
                    )}
                  </td>

                  {/* Building */}
                  <td className="py-3.5 px-3 text-muted-foreground">
                    <span
                      onClick={(e) => {
                        if (p.buildingId) {
                          e.stopPropagation();
                          navigate(ROUTES.buildings.details(p.buildingId));
                        }
                      }}
                      className="hover:text-foreground hover:underline"
                    >
                      {p.buildingName || '—'}
                    </span>
                  </td>

                  {/* Apartment Unit */}
                  <td className="py-3.5 px-3 text-muted-foreground font-mono">
                    {p.apartmentNumber ? t('financials.unitPrefix', { number: p.apartmentNumber }) : '—'}
                  </td>

                  {/* Amount Due */}
                  <td className="py-3.5 px-3 font-semibold font-mono text-foreground">
                    {formatCurrency(p.amountDue, { currency: p.currency })}
                  </td>

                  {/* Amount Paid */}
                  <td className="py-3.5 px-3 font-mono text-success font-medium">
                    {formatCurrency(p.amountPaid, { currency: p.currency })}
                  </td>

                  {/* Remaining */}
                  <td className={`py-3.5 px-3 font-semibold font-mono ${remaining > 0 ? 'text-danger' : 'text-muted-foreground'}`}>
                    {formatCurrency(remaining, { currency: p.currency })}
                  </td>

                  {/* Due Date */}
                  <td className="py-3.5 px-3 text-muted-foreground font-mono">
                    {p.dueDate ? formatDate(p.dueDate, language) : '—'}
                  </td>

                  {/* Status Badge */}
                  <td className="py-3.5 px-3 text-center">
                    <StatusBadge label={statusCfg.label} variant={statusCfg.variant} size="sm" />
                  </td>

                  {/* Actions */}
                  <td className="py-3.5 px-4 text-end" onClick={(e) => e.stopPropagation()}>
                    <Button
                      variant="ghost"
                      size="icon"
                      title={t('common.viewDetails') || 'عرض التفاصيل'}
                      aria-label={t('common.viewDetails') || 'عرض التفاصيل'}
                      onClick={() => onSelectPayment(p)}
                      className="h-8 w-8 text-muted-foreground hover:text-foreground"
                    >
                      <Eye className="w-4 h-4" />
                    </Button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Keyset Pagination Toolbar */}
      <div className="flex items-center justify-between px-2 pt-1 text-xs text-muted-foreground">
        <span>
          {t('financials.showingPage', { page: currentPageIndex }) || `الصفحة ${currentPageIndex}`}
        </span>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={onPrevPage}
            disabled={!hasPrevPage}
            className="gap-1 h-8 px-3 text-xs"
          >
            <ChevronRight className="w-3.5 h-3.5 rtl:rotate-0 rotate-180" />
            <span>{t('table.previous') || 'السابق'}</span>
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={onNextPage}
            disabled={!hasNextPage}
            className="gap-1 h-8 px-3 text-xs"
          >
            <span>{t('table.next') || 'التالي'}</span>
            <ChevronLeft className="w-3.5 h-3.5 rtl:rotate-0 rotate-180" />
          </Button>
        </div>
      </div>
    </div>
  );
}
