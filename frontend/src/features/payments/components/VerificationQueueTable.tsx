import React from 'react';
import { useTranslation } from '@/shared/i18n';
import { PaymentVerificationQueueItem } from '../types/payments.types';
import { DataTable } from '@/shared/components/ui';
import { CheckCircle2, XCircle, Clock } from 'lucide-react';

interface VerificationQueueTableProps {
  items: PaymentVerificationQueueItem[];
  isLoading: boolean;
  onSelect: (item: PaymentVerificationQueueItem) => void;
}

export function VerificationQueueTable({ items, isLoading, onSelect }: VerificationQueueTableProps) {
  const { t, language, direction } = useTranslation();
  const isRtl = direction === 'rtl';

  const formatPaymentMethod = (method: any) => {
    if (method === null || method === undefined) return '-';
    if (typeof method === 'number' || (!isNaN(Number(method)) && String(method).trim() !== '')) {
      const num = Number(method);
      switch (num) {
        case 0: return t('Cash');
        case 1: return t('Bank Transfer');
        case 2: return t('Cheque');
        case 3: return t('eFAWATEERCOM');
        case 4: return t('CliQ');
        default: return String(method);
      }
    }
    const str = String(method).trim();
    const lower = str.toLowerCase().replace(/[^a-z]/g, '');
    if (lower === 'cash') return t('Cash');
    if (lower === 'banktransfer') return t('Bank Transfer');
    if (lower === 'cheque') return t('Cheque');
    if (lower === 'efawateercom') return t('eFAWATEERCOM');
    if (lower === 'cliq' || lower === 'cli_q') return t('CliQ');
    return t(str);
  };

  return (
    <div className="bg-card border border-border rounded-xl shadow-sm overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-sm text-left">
          <thead className="bg-muted/50 text-muted-foreground uppercase text-xs">
            <tr>
              <th className={`px-6 py-4 font-medium ${isRtl ? 'text-right' : 'text-left'}`}>{t('Tenant')}</th>
              <th className={`px-6 py-4 font-medium ${isRtl ? 'text-right' : 'text-left'}`}>{t('Contract')}</th>
              <th className={`px-6 py-4 font-medium ${isRtl ? 'text-right' : 'text-left'}`}>{t('Amount')}</th>
              <th className={`px-6 py-4 font-medium ${isRtl ? 'text-right' : 'text-left'}`}>{t('Payment Method')}</th>
              <th className={`px-6 py-4 font-medium ${isRtl ? 'text-right' : 'text-left'}`}>{t('Submitted At')}</th>
              <th className={`px-6 py-4 font-medium ${isRtl ? 'text-right' : 'text-left'}`}>{t('Action')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border hidden md:table-row-group">
            {isLoading ? (
              <tr>
                <td colSpan={6} className="px-6 py-8 text-center text-muted-foreground animate-pulse">
                  {t('Loading...')}
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-12 text-center text-muted-foreground">
                  <div className="flex flex-col items-center justify-center space-y-3">
                    <div className="p-3 bg-muted rounded-full">
                      <CheckCircle2 className="h-8 w-8 text-muted-foreground" />
                    </div>
                    <p className="text-base font-medium text-foreground">{t('No payment verification requests')}</p>
                    <p className="text-sm max-w-sm">{t('New tenant payment submissions requiring verification will appear here.')}</p>
                  </div>
                </td>
              </tr>
            ) : (
              items.map((item) => (
                <tr key={item.paymentSubmissionId} className="hover:bg-muted/30 transition-colors">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="font-medium text-foreground">{item.tenantName || t('Unknown Tenant')}</div>
                    <div className="text-xs text-muted-foreground">{item.buildingName} - {item.apartmentNumber}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="text-muted-foreground">{item.contractNumber || '-'}</span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="font-medium text-foreground">{item.amountDue.toLocaleString()} {item.currency}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-secondary text-secondary-foreground">
                      {formatPaymentMethod(item.paymentMethod)}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-muted-foreground">
                    {new Date(item.submittedAt).toLocaleDateString(language)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <button
                      onClick={() => onSelect(item)}
                      className="inline-flex items-center justify-center px-3 py-1.5 border border-primary text-primary hover:bg-primary hover:text-primary-foreground rounded-md text-sm font-medium transition-colors"
                    >
                      {t('View / Verify')}
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
        
        {/* Mobile View: Cards */}
        <div className="md:hidden flex flex-col divide-y divide-border">
          {isLoading ? (
            <div className="px-4 py-8 text-center text-muted-foreground animate-pulse">
              {t('Loading...')}
            </div>
          ) : items.length === 0 ? (
            <div className="px-4 py-12 text-center text-muted-foreground">
              <div className="flex flex-col items-center justify-center space-y-3">
                <div className="p-3 bg-muted rounded-full">
                  <CheckCircle2 className="h-8 w-8 text-muted-foreground" />
                </div>
                <p className="text-base font-medium text-foreground">{t('No payment verification requests')}</p>
                <p className="text-sm">{t('New tenant payment submissions requiring verification will appear here.')}</p>
              </div>
            </div>
          ) : (
            items.map((item) => (
              <div key={item.paymentSubmissionId} className="p-4 flex flex-col space-y-3 hover:bg-muted/30 transition-colors">
                <div className="flex justify-between items-start">
                  <div>
                    <div className="font-medium text-foreground">{item.tenantName || t('Unknown Tenant')}</div>
                    <div className="text-xs text-muted-foreground">{item.buildingName} - {item.apartmentNumber}</div>
                  </div>
                  <div className="font-medium text-primary text-right">
                    {item.amountDue.toLocaleString()} {item.currency}
                  </div>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{item.contractNumber || '-'}</span>
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-medium bg-secondary text-secondary-foreground">
                    {formatPaymentMethod(item.paymentMethod)}
                  </span>
                </div>
                <div className="flex items-center justify-between pt-2 border-t border-border/50">
                  <div className="text-xs text-muted-foreground flex items-center">
                    <Clock className="h-3 w-3 mr-1 rtl:ml-1 rtl:mr-0" />
                    {new Date(item.submittedAt).toLocaleDateString(language)}
                  </div>
                  <button
                    onClick={() => onSelect(item)}
                    className="inline-flex items-center justify-center px-3 py-1 border border-primary text-primary hover:bg-primary hover:text-primary-foreground rounded-md text-sm font-medium transition-colors"
                  >
                    {t('Verify')}
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
