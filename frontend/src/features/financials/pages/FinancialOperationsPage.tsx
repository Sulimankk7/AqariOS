import React, { useState } from 'react';
import { PageContainer } from '@/shared/components/layout/PageContainer';
import { Tabs, type TabItem } from '@/shared/components/ui/NavigationUI';
import { useTranslation } from '@/shared/i18n';
import { FinancialSummaryCards } from '../components/FinancialSummaryCards';
import { RentPaymentFilters } from '../components/RentPaymentFilters';
import { RentCollectionTable } from '../components/RentCollectionTable';
import { FinancialPaymentDetailsDrawer } from '../components/FinancialPaymentDetailsDrawer';
import { ExpenseFilters } from '../components/ExpenseFilters';
import { ExpenseTable } from '../components/ExpenseTable';
import { ExpenseDetailsDrawer } from '../components/ExpenseDetailsDrawer';
import { useExpenses, useRentPayments } from '../hooks/useRentPayments';
import { DueDateStatus, type ExpenseDto, type ExpenseFilterParams, type RentPaymentDto, type RentPaymentFilterParams } from '../types/financials.types';
import { RefreshCw, Calculator } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useQueryClient } from '@tanstack/react-query';

export function FinancialOperationsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState<'records' | 'expenses'>('records');
  const [selectedPayment, setSelectedPayment] = useState<RentPaymentDto | null>(null);
  const [selectedExpense, setSelectedExpense] = useState<ExpenseDto | null>(null);

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

  const [expenseFilters, setExpenseFilters] = useState<ExpenseFilterParams>({ pageSize: 50 });
  const [expenseCursorHistory, setExpenseCursorHistory] = useState<Array<{ id: string | null; expenseDate: string | null }>>([{ id: null, expenseDate: null }]);
  const [expensePageIndex, setExpensePageIndex] = useState(1);
  const expenseQuery = useExpenses(expenseFilters);

  const tabs: TabItem[] = [
    {
      id: 'records',
      label: t('financials.financialRecordsTab'),
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
      expenseQuery.refetch(),
      queryClient.invalidateQueries({ queryKey: ['dashboard', 'summary'] }),
    ]);
  };

  const handlePendingReviewClick = () => {
    setActiveTab('records');
    handleFiltersChange({
      buildingId: null,
      status: DueDateStatus.PendingVerification,
      dateFrom: null,
      dateTo: null,
      searchTerm: null,
      lastSeenId: null,
      lastSeenDueDate: null,
      pageSize: filters.pageSize || 50,
    });
    window.requestAnimationFrame(() => document.getElementById('financial-records')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  };

  const lastPayment = payments?.[payments.length - 1];
  const hasNextPage = Boolean(payments && payments.length === (filters.pageSize || 50) && lastPayment?.dueDate);
  const hasPrevPage = cursorHistory.length > 1;

  const handleExpenseFiltersChange = (next: ExpenseFilterParams) => {
    setExpenseFilters(next);
    setExpenseCursorHistory([{ id: null, expenseDate: null }]);
    setExpensePageIndex(1);
  };

  const handleNextExpensePage = () => {
    const items = expenseQuery.data;
    if (!items?.length) return;
    const last = items[items.length - 1];
    const cursor = { id: last.id, expenseDate: last.expenseDate };
    setExpenseCursorHistory((history) => [...history, cursor]);
    setExpensePageIndex((page) => page + 1);
    setExpenseFilters((current) => ({ ...current, lastSeenId: cursor.id, lastSeenExpenseDate: cursor.expenseDate }));
  };

  const handlePreviousExpensePage = () => {
    if (expenseCursorHistory.length <= 1) return;
    const history = expenseCursorHistory.slice(0, -1);
    const cursor = history[history.length - 1];
    setExpenseCursorHistory(history);
    setExpensePageIndex((page) => Math.max(1, page - 1));
    setExpenseFilters((current) => ({ ...current, lastSeenId: cursor.id, lastSeenExpenseDate: cursor.expenseDate }));
  };

  return (
    <PageContainer>
      {/* Header */}
      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-xl md:text-2xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Calculator className="w-6 h-6 text-primary" />
            <span>{t('financials.operationsTitle')}</span>
          </h1>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={isRefetching || isLoading || expenseQuery.isRefetching}
            className="text-xs gap-1.5 h-8"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefetching || expenseQuery.isRefetching ? 'animate-spin' : ''}`} />
            <span>{t('common.refresh')}</span>
          </Button>

        </div>
      </div>

      {/* Financial KPI Summary Cards */}
      <FinancialSummaryCards onPendingClick={handlePendingReviewClick} />

      {/* Main Tabs */}
      <Tabs
        items={tabs}
        activeId={activeTab}
        onChange={(id) => setActiveTab(id as 'records' | 'expenses')}
        className="pt-2"
      />

      {activeTab === 'records' && (
        <div id="financial-records" className="space-y-3 pt-1 scroll-mt-4">
          <RentPaymentFilters
            filters={filters}
            onFiltersChange={handleFiltersChange}
          />

          <RentCollectionTable
            payments={payments || []}
            isLoading={isLoading}
            isError={isError}
            onRetry={() => refetch()}
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

      {activeTab === 'expenses' && (
        <div className="space-y-4 pt-1">
          <ExpenseFilters filters={expenseFilters} onChange={handleExpenseFiltersChange} />
          <ExpenseTable
            expenses={expenseQuery.data || []}
            isLoading={expenseQuery.isLoading}
            isError={expenseQuery.isError}
            currentPage={expensePageIndex}
            hasPrevious={expenseCursorHistory.length > 1}
            hasNext={Boolean(expenseQuery.data?.length === (expenseFilters.pageSize || 50))}
            onPrevious={handlePreviousExpensePage}
            onNext={handleNextExpensePage}
            onRetry={() => expenseQuery.refetch()}
            onSelect={setSelectedExpense}
          />
        </div>
      )}

      {/* Payment Details Drawer */}
      <FinancialPaymentDetailsDrawer
        payment={selectedPayment}
        onClose={() => setSelectedPayment(null)}
      />
      <ExpenseDetailsDrawer expense={selectedExpense} onClose={() => setSelectedExpense(null)} />
    </PageContainer>
  );
}

export default FinancialOperationsPage;
