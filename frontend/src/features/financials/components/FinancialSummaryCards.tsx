import React from 'react';
import { Wallet, CircleDollarSign, AlertTriangle, Receipt, Clock } from 'lucide-react';
import { useDashboardSummary } from '@/features/dashboard/hooks/useDashboardSummary';
import { useVerificationQueue } from '@/features/payments/hooks/usePaymentVerifications';
import { StatCard } from '@/shared/components/ui/StatCard';
import { Skeleton } from '@/shared/components/ui/Feedback';
import { useTranslation } from '@/shared/i18n';
import { formatFinancialCurrency, formatFinancialNumber } from '../utils/financialRecord';

export function FinancialSummaryCards({ onPendingClick }: { onPendingClick?: () => void }) {
  const { t, language } = useTranslation();
  const { data, isLoading, isError } = useDashboardSummary();
  const { data: verificationsData } = useVerificationQueue();

  const pendingCount = verificationsData?.pages?.[0]?.items?.length ?? 0;

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-3">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="p-4 rounded-lg border border-border bg-card space-y-2">
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-8 w-36" />
            <Skeleton className="h-3 w-24" />
          </div>
        ))}
      </div>
    );
  }

  if (isError || !data) {
    return null;
  }

  const { payments, financials } = data;

  return (
    <div className="grid grid-cols-2 lg:grid-cols-5 gap-3">
      <StatCard
        title={t('financials.collectedThisMonth')}
        value={formatFinancialCurrency(payments.collectedThisMonth, 'JOD', language)}
        icon={Wallet}
        variant="success"
      />
      <StatCard
        title={t('financials.outstandingAmount')}
        value={formatFinancialCurrency(payments.outstandingAmount, 'JOD', language)}
        icon={CircleDollarSign}
        variant={payments.outstandingAmount > 0 ? 'warning' : 'default'}
      />
      <StatCard
        title={t('financials.overduePayments')}
        value={formatFinancialNumber(payments.overduePayments)}
        icon={AlertTriangle}
        variant={payments.overduePayments > 0 ? 'danger' : 'default'}
      />
      <StatCard
        title={t('financials.pendingVerifications')}
        value={formatFinancialNumber(pendingCount)}
        icon={Clock}
        variant={pendingCount > 0 ? 'info' : 'default'}
        onClick={onPendingClick}
      />
      <StatCard
        title={t('financials.expensesThisMonth')}
        value={formatFinancialCurrency(financials.expensesThisMonth, 'JOD', language)}
        icon={Receipt}
        variant="default"
      />
    </div>
  );
}
