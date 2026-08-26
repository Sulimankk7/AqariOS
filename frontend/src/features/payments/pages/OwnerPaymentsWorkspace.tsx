import React, { useState } from 'react';
import { useTranslation } from '@/shared/i18n';
import { useVerificationQueue } from '../hooks/usePaymentVerifications';
import { PaymentVerificationQueueItem } from '../types/payments.types';
import { VerificationQueueTable } from '../components/VerificationQueueTable';
import { VerificationDetailsDrawer } from '../components/VerificationDetailsDrawer';

export default function OwnerPaymentsWorkspace() {
  const { t } = useTranslation();
  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } = useVerificationQueue();
  const [selectedItem, setSelectedItem] = useState<PaymentVerificationQueueItem | null>(null);

  // Flatten the pages from react-query infinite scroll
  const items = data?.pages.flatMap(page => page.items) || [];

  return (
    <div className="flex flex-col h-full bg-background p-4 md:p-8 space-y-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{t('payments.title')}</h1>
          <p className="text-muted-foreground mt-1">
            {t('payments.subtitle')}
          </p>
        </div>
      </div>

      <div className="flex-1 overflow-hidden flex flex-col space-y-4">
        <VerificationQueueTable 
          items={items} 
          isLoading={isLoading} 
          onSelect={setSelectedItem} 
        />
        
        {hasNextPage && (
          <div className="flex justify-center pt-4">
            <button
              onClick={() => fetchNextPage()}
              disabled={isFetchingNextPage}
              className="px-4 py-2 bg-secondary text-secondary-foreground rounded-md text-sm font-medium hover:bg-secondary/80 transition-colors disabled:opacity-50"
            >
              {isFetchingNextPage ? t('Loading more...') : t('Load More')}
            </button>
          </div>
        )}
      </div>

      <VerificationDetailsDrawer 
        item={selectedItem} 
        onClose={() => setSelectedItem(null)} 
      />
    </div>
  );
}
