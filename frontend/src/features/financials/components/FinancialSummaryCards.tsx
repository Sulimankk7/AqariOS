import React from 'react';
import { Wallet, CircleDollarSign, AlertTriangle, Receipt } from 'lucide-react';
import { useDashboardSummary } from '@/features/dashboard/hooks/useDashboardSummary';
import { StatCard } from '@/shared/components/ui/StatCard';
import { Skeleton } from '@/shared/components/ui/Feedback';
import { useTranslation } from '@/shared/i18n';

export function FinancialSummaryCards() {
  const { t, formatCurrency } = useTranslation();
  const { data, isLoading, isError } = useDashboardSummary();

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="p-5 rounded-lg border border-border bg-card space-y-3">
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
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <StatCard
        title={t('financials.collectedThisMonth')}
        value={formatCurrency(payments.collectedThisMonth)}
        description={t('financials.collectedThisMonth')}
        icon={Wallet}
        variant="success"
      />
      <StatCard
        title={t('financials.outstandingAmount')}
        value={formatCurrency(payments.outstandingAmount)}
        description={t('financials.outstandingAmount')}
        icon={CircleDollarSign}
        variant={payments.outstandingAmount > 0 ? 'warning' : 'default'}
      />
      <StatCard
        title={t('financials.overduePayments')}
        value={payments.overduePayments}
        description={t('financials.overduePayments')}
        icon={AlertTriangle}
        variant={payments.overduePayments > 0 ? 'danger' : 'default'}
      />
      <StatCard
        title={t('financials.expensesThisMonth')}
        value={formatCurrency(financials.expensesThisMonth)}
        description={t('financials.expensesThisMonth')}
        icon={Receipt}
        variant="default"
      />
    </div>
  );
}
