import React from 'react';
import { AlertCircle, ChevronLeft, ChevronRight, Eye, ReceiptText } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { EmptyState, Skeleton } from '@/shared/components/ui/Feedback';
import { useTranslation } from '@/shared/i18n';
import { ExpenseCategory, ExpensePaymentMethod, type ExpenseDto } from '../types/financials.types';
import { expenseCategoryValue, expensePaymentMethodValue, formatFinancialCurrency, formatFinancialDate, formatFinancialNumber } from '../utils/financialRecord';

interface ExpenseTableProps {
  expenses: ExpenseDto[];
  isLoading: boolean;
  isError: boolean;
  currentPage: number;
  hasPrevious: boolean;
  hasNext: boolean;
  onPrevious: () => void;
  onNext: () => void;
  onRetry: () => void;
  onSelect: (expense: ExpenseDto) => void;
}

export function ExpenseTable(props: ExpenseTableProps) {
  const { t, language } = useTranslation();
  const categoryLabel = (value: ExpenseDto['category']) => {
    const category = expenseCategoryValue(value);
    return category === null ? t('common.unknown') : t(`financials.expenseCategory${ExpenseCategory[category]}`);
  };
  const methodLabel = (value: ExpenseDto['paymentMethod']) => {
    switch (expensePaymentMethodValue(value)) {
      case ExpensePaymentMethod.Cash: return t('financials.paymentMethodCash');
      case ExpensePaymentMethod.BankTransfer: return t('financials.paymentMethodBankTransfer');
      case ExpensePaymentMethod.Cheque: return t('financials.paymentMethodCheque');
      case ExpensePaymentMethod.Other: return t('financials.paymentMethodOther');
      default: return t('common.unknown');
    }
  };

  if (props.isLoading) return <div className="bg-card rounded-lg border border-border p-4 space-y-3">{[1, 2, 3, 4, 5].map((item) => <Skeleton key={item} className="h-12 w-full" />)}</div>;
  if (props.isError) return <div className="bg-card rounded-lg border border-danger/30 p-8 text-center space-y-3"><AlertCircle className="w-8 h-8 text-danger mx-auto" /><p className="text-sm font-semibold text-danger">{t('financials.expenseLoadError')}</p><Button variant="outline" size="sm" onClick={props.onRetry}>{t('common.retry')}</Button></div>;
  if (!props.expenses.length) return <EmptyState icon={ReceiptText} title={t('financials.emptyExpensesTitle')} />;

  return (
    <div className="space-y-4">
      <div className="bg-card rounded-lg border border-border overflow-x-auto shadow-2xs">
        <table className="w-full min-w-[800px] text-xs text-start">
          <thead><tr className="border-b border-border bg-secondary/50 text-muted-foreground font-semibold">
            <th className="py-3 px-4 text-start">{t('financials.expense')}</th>
            <th className="py-3 px-3 text-start">{t('financials.expenseCategory')}</th>
            <th className="py-3 px-3 text-start">{t('financials.vendor')}</th>
            <th className="py-3 px-3 text-start">{t('financials.amount')}</th>
            <th className="py-3 px-3 text-start">{t('financials.paymentMethod')}</th>
            <th className="py-3 px-3 text-start">{t('financials.expenseDate')}</th>
            <th className="py-3 px-4 text-end">{t('common.actions')}</th>
          </tr></thead>
          <tbody className="divide-y divide-border">
            {props.expenses.map((expense) => (
              <tr key={expense.id} onClick={() => props.onSelect(expense)} className="hover:bg-secondary/30 cursor-pointer">
                <td className="py-3 px-4"><span className="block font-medium text-foreground max-w-64 truncate">{expense.description}</span>{expense.invoiceNumber && <bdi dir="ltr" className="block text-[10px] text-muted-foreground font-mono mt-0.5">{expense.invoiceNumber}</bdi>}</td>
                <td className="py-3.5 px-3 text-muted-foreground">{categoryLabel(expense.category)}</td>
                <td className="py-3.5 px-3 text-muted-foreground">{expense.vendorName || '—'}</td>
                <td className="py-3 px-3 font-semibold font-mono text-foreground"><bdi dir="ltr">{formatFinancialCurrency(expense.amount, expense.currency, language)}</bdi></td>
                <td className="py-3.5 px-3 text-muted-foreground">{methodLabel(expense.paymentMethod)}</td>
                <td className="py-3 px-3 text-muted-foreground font-mono" dir="ltr">{formatFinancialDate(expense.expenseDate)}</td>
                <td className="py-3.5 px-4 text-end" onClick={(event) => event.stopPropagation()}><Button variant="ghost" size="icon" onClick={() => props.onSelect(expense)} aria-label={t('common.viewDetails')} className="h-8 w-8"><Eye className="w-4 h-4" /></Button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex items-center justify-between px-2 text-xs text-muted-foreground">
        <span>{t('financials.showingPage', { page: formatFinancialNumber(props.currentPage) })}</span>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={props.onPrevious} disabled={!props.hasPrevious} className="h-8 gap-1"><ChevronRight className="w-3.5 h-3.5 rtl:rotate-0 rotate-180" />{t('table.previous')}</Button>
          <Button variant="outline" size="sm" onClick={props.onNext} disabled={!props.hasNext} className="h-8 gap-1">{t('table.next')}<ChevronLeft className="w-3.5 h-3.5 rtl:rotate-0 rotate-180" /></Button>
        </div>
      </div>
    </div>
  );
}
