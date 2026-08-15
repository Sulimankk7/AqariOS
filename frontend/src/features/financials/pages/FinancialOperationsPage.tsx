import React, { useState } from 'react';
import { PageContainer } from '@/shared/components/layout/PageContainer';
import { PageHeader } from '@/shared/components/ui/Headers';
import { Tabs, type TabItem } from '@/shared/components/ui/NavigationUI';
import { useTranslation } from '@/shared/i18n';
import { FinancialSummaryCards } from '../components/FinancialSummaryCards';
import { RentPaymentFilters } from '../components/RentPaymentFilters';
import { RentCollectionTable } from '../components/RentCollectionTable';
import { FinancialPaymentDetailsDrawer } from '../components/FinancialPaymentDetailsDrawer';
import { useRentPayments } from '../hooks/useRentPayments';
import type { RentPaymentDto, RentPaymentFilterParams } from '../types/financials.types';
import { Receipt, RefreshCw, Calculator, ArrowUpRight } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useQueryClient } from '@tanstack/react-query';

export function FinancialOperationsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState<'collection' | 'expenses'>('collection');
  const [selectedPayment, setSelectedPayment] = useState<RentPaymentDto | null>(null);

  // Filter state
  const [filters, setFilters] = useState<RentPaymentFilterParams>({
    buildingId: null,
    status: null,
    dateFrom: null,
    dateTo: null,
    searchTerm: null,
    lastSeenId: null,
    lastSeenDueDate: null,
    pageSize: 50,
  });

  // Cursor pagination stack
  const [cursorHistory, setCursorHistory] = useState<Array<{ id: string | null; dueDate: string | null }>>([
    { id: null, dueDate: null },
  ]);
  const [currentPageIndex, setCurrentPageIndex] = useState(1);

  const { data: payments, isLoading, isError, refetch, isRefetching } = useRentPayments(filters);

  const tabs: TabItem[] = [
    {
      id: 'collection',
      label: t('financials.rentCollectionTab'),
    },
    {
      id: 'expenses',
      label: t('financials.expensesTab'),
    },
  ];

  const handleFiltersChange = (newFilters: RentPaymentFilterParams) => {
    setFilters(newFilters);
    // Reset pagination on filter change
    setCursorHistory([{ id: null, dueDate: null }]);
    setCurrentPageIndex(1);
  };

  const handleNextPage = () => {
    if (!payments || payments.length === 0) return;
    const lastItem = payments[payments.length - 1];

    const nextCursor = { id: lastItem.id, dueDate: lastItem.dueDate };
    setCursorHistory((prev) => [...prev, nextCursor]);
    setCurrentPageIndex((prev) => prev + 1);

    setFilters((prev) => ({
      ...prev,
      lastSeenId: nextCursor.id,
      lastSeenDueDate: nextCursor.dueDate,
    }));
  };

  const handlePrevPage = () => {
    if (cursorHistory.length <= 1) return;

    const newHistory = cursorHistory.slice(0, cursorHistory.length - 1);
    const targetCursor = newHistory[newHistory.length - 1];

    setCursorHistory(newHistory);
    setCurrentPageIndex((prev) => Math.max(1, prev - 1));

    setFilters((prev) => ({
      ...prev,
      lastSeenId: targetCursor.id,
      lastSeenDueDate: targetCursor.dueDate,
    }));
  };

  const handleRefresh = async () => {
    await Promise.all([
      refetch(),
      queryClient.invalidateQueries({ queryKey: ['dashboard', 'summary'] }),
    ]);
  };

  const hasNextPage = Boolean(payments && payments.length === (filters.pageSize || 50));
  const hasPrevPage = cursorHistory.length > 1;

  return (
    <PageContainer>
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl md:text-2xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Calculator className="w-6 h-6 text-primary" />
            <span>{t('financials.operationsTitle')}</span>
          </h1>
          <p className="text-xs md:text-sm text-muted-foreground mt-1">
            {t('financials.operationsSubtitle')}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={isRefetching || isLoading}
            className="text-xs gap-1.5 h-8"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefetching ? 'animate-spin' : ''}`} />
            <span>{t('common.refresh')}</span>
          </Button>
        </div>
      </div>

      {/* Financial KPI Summary Cards */}
      <FinancialSummaryCards />

      {/* Main Tabs */}
      <Tabs
        items={tabs}
        activeId={activeTab}
        onChange={(id) => setActiveTab(id as 'collection' | 'expenses')}
        className="pt-2"
      />

      {/* Tab 1: Rent Collection */}
      {activeTab === 'collection' && (
        <div className="space-y-4 pt-1">
          <RentPaymentFilters
            filters={filters}
            onFiltersChange={handleFiltersChange}
          />

          <RentCollectionTable
            payments={payments || []}
            isLoading={isLoading}
            isError={isError}
            onSelectPayment={setSelectedPayment}
            onNextPage={handleNextPage}
            onPrevPage={handlePrevPage}
            hasNextPage={hasNextPage}
            hasPrevPage={hasPrevPage}
            currentPageIndex={currentPageIndex}
            onResetFilters={() => handleFiltersChange({
              buildingId: null,
              status: null,
              dateFrom: null,
              dateTo: null,
              searchTerm: null,
              lastSeenId: null,
              lastSeenDueDate: null,
              pageSize: 50,
            })}
          />
        </div>
      )}

      {/* Tab 2: Operating Expenses */}
      {activeTab === 'expenses' && (
        <div className="bg-card rounded-lg border border-border p-8 text-center space-y-4">
          <Receipt className="w-10 h-10 text-muted-foreground mx-auto" />
          <div className="max-w-md mx-auto space-y-1.5">
            <h3 className="text-base font-semibold text-foreground">
              {t('financials.expensesTab')}
            </h3>
            <p className="text-xs text-muted-foreground">
              {t('financials.expensesSectionNotice')}
            </p>
          </div>
        </div>
      )}

      {/* Payment Details Drawer */}
      <FinancialPaymentDetailsDrawer
        payment={selectedPayment}
        onClose={() => setSelectedPayment(null)}
      />
    </PageContainer>
  );
}

export default FinancialOperationsPage;
