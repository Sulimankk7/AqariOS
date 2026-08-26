import React from 'react';
import { Calendar, Download, FileText, Loader2, Receipt, Store, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/app/components/ui/button';
import { ErrorState, Skeleton } from '@/shared/components/ui/Feedback';
import { useTranslation } from '@/shared/i18n';
import { filesApi } from '@/shared/services/files.api';
import { useExpenseDetails } from '../hooks/useRentPayments';
import { ExpenseCategory, ExpensePaymentMethod, type ExpenseDto } from '../types/financials.types';
import { expenseCategoryValue, expensePaymentMethodValue, formatFinancialCurrency, formatFinancialDate } from '../utils/financialRecord';

export function ExpenseDetailsDrawer({ expense, onClose }: { expense: ExpenseDto | null; onClose: () => void }) {
  const { t, language } = useTranslation();
  const { data, isLoading, isError, refetch } = useExpenseDetails(expense?.id || null);
  const [downloadingId, setDownloadingId] = React.useState<string | null>(null);
  if (!expense) return null;
  const record = data ?? expense;
  const category = expenseCategoryValue(record.category);
  const method = expensePaymentMethodValue(record.paymentMethod);
  const methodLabel = method === ExpensePaymentMethod.Cash ? t('financials.paymentMethodCash')
    : method === ExpensePaymentMethod.BankTransfer ? t('financials.paymentMethodBankTransfer')
    : method === ExpensePaymentMethod.Cheque ? t('financials.paymentMethodCheque')
    : t('financials.paymentMethodOther');

  const download = async (fileId: string) => {
    setDownloadingId(fileId);
    try {
      const file = await filesApi.getFileDownloadUrl(fileId, false);
      window.open(file.downloadUrl, '_blank', 'noopener,noreferrer');
    } catch {
      toast.error(t('financials.expenseReceiptDownloadError'));
    } finally {
      setDownloadingId(null);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex justify-end" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <div className="w-full sm:max-w-[500px] h-full bg-card border-s border-border shadow-2xl flex flex-col overflow-hidden">
        <div className="px-4 py-3 border-b border-border flex justify-between items-center">
          <div><h2 className="text-base font-bold text-foreground">{t('financials.expenseDetails')}</h2>{record.invoiceNumber && <bdi dir="ltr" className="block max-w-80 truncate text-[11px] font-mono text-muted-foreground">{record.invoiceNumber}</bdi>}</div>
          <button onClick={onClose} className="p-2 rounded-full hover:bg-secondary text-muted-foreground" aria-label={t('common.close')}><X className="w-5 h-5" /></button>
        </div>
        <div className="p-4 space-y-4 flex-1 overflow-y-auto overflow-x-hidden">
          {isLoading && <><Skeleton className="h-28 w-full" /><Skeleton className="h-48 w-full" /></>}
          {isError && <ErrorState title={t('financials.expenseLoadError')} onRetry={() => refetch()} />}
          {!isLoading && !isError && (
            <>
              <div className="p-3 rounded-lg border border-border bg-secondary/40 text-center">
                <span className="text-xs text-muted-foreground">{t('financials.expenseAmount')}</span>
                <bdi dir="ltr" className="block text-xl font-bold font-mono mt-1">{formatFinancialCurrency(record.amount, record.currency, language)}</bdi>
              </div>
              <div className="rounded-lg border border-border divide-y divide-border text-xs">
                <Row icon={FileText} label={t('financials.description')} value={record.description} />
                <Row icon={Receipt} label={t('financials.expenseCategory')} value={category === null ? String(record.category) : t(`financials.expenseCategory${ExpenseCategory[category]}`)} />
                <Row icon={Store} label={t('financials.vendor')} value={record.vendorName || '—'} />
                <Row icon={Calendar} label={t('financials.expenseDate')} value={formatFinancialDate(record.expenseDate)} mono />
                <Row icon={Receipt} label={t('financials.paymentMethod')} value={methodLabel} />
                {record.invoiceNumber && <Row icon={FileText} label={t('financials.invoiceNumber')} value={record.invoiceNumber} mono />}
              </div>
              {record.notes && <section className="space-y-2"><h3 className="text-xs font-semibold text-muted-foreground uppercase">{t('financials.notes')}</h3><p className="p-3 rounded-lg border border-border text-xs whitespace-pre-wrap">{record.notes}</p></section>}
              {data?.receipts?.length ? <section className="space-y-3">
                <h3 className="text-xs font-semibold text-muted-foreground uppercase">{t('financials.expenseReceipts')}</h3>
                {data.receipts.map((receipt) => (
                  <div key={receipt.id} className="p-3 rounded-lg border border-border flex items-center justify-between gap-3">
                    <div><bdi dir="ltr" className="block text-xs font-semibold font-mono">{receipt.receiptNumber}</bdi><bdi dir="ltr" className="block text-[11px] text-muted-foreground mt-1">{formatFinancialCurrency(receipt.amount, record.currency, language)} · {formatFinancialDate(receipt.issuedAt)}</bdi></div>
                    <Button variant="outline" size="sm" onClick={() => download(receipt.fileId)} disabled={downloadingId === receipt.fileId} className="h-8 text-xs gap-1.5">
                      {downloadingId === receipt.fileId ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Download className="w-3.5 h-3.5" />}{t('financials.downloadReceipt')}
                    </Button>
                  </div>
                ))}
              </section> : null}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function Row({ icon: Icon, label, value, mono = false }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string; mono?: boolean }) {
  return <div className="p-3 flex items-center justify-between gap-4"><span className="text-muted-foreground flex items-center gap-2"><Icon className="w-3.5 h-3.5" />{label}</span><bdi dir={mono ? 'ltr' : undefined} className={`font-medium text-foreground text-end ${mono ? 'font-mono' : ''}`}>{value}</bdi></div>;
}
