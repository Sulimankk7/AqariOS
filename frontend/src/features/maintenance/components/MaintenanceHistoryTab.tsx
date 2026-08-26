import React from 'react';
import { useMaintenanceHistory } from '../hooks/useMaintenance';
import { Skeleton } from '@/app/components/ui/skeleton';
import { MaintenanceStatusBadge } from './MaintenanceStatusBadge';
import { History } from 'lucide-react';
import { useTranslation } from '@/shared/i18n';
import { useMaintenanceActorName } from '../hooks/useMaintenanceActorName';

export const MaintenanceHistoryTab = ({ requestId }: { requestId: string }) => {
  const { t, formatDate } = useTranslation();
  const { data: history, isLoading } = useMaintenanceHistory(requestId);
  const actorName = useMaintenanceActorName();

  if (isLoading) {
    return (
      <div className="space-y-4 py-4">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    );
  }

  if (!history?.length) {
    return (
      <div className="text-center py-8 text-muted-foreground flex flex-col items-center pt-8">
        <History className="w-8 h-8 mb-2 opacity-50" />
        <p>{t('maintenance.noHistory')}</p>
      </div>
    );
  }

  return (
    <div className="py-4 space-y-4">
      {history.map((entry, index) => (
        <div key={entry.id} className="relative pl-4 border-r-2 border-primary/20 rtl:pl-0 rtl:pr-4 rtl:border-r-0 rtl:border-l-2 text-sm">
          <div className="absolute w-2 h-2 bg-primary rounded-full top-1.5 -right-[5px] rtl:-left-[5px] rtl:-right-auto border-2 border-background" />
          <div className="flex justify-between items-start mb-1">
            <div className="flex items-center gap-2 flex-wrap">
              {entry.previousStatus ? (
                <>
                  <MaintenanceStatusBadge status={entry.previousStatus} />
                  <span className="text-muted-foreground">←</span>
                  <MaintenanceStatusBadge status={entry.newStatus} />
                </>
              ) : (
                <>
                  <span className="text-muted-foreground">{t('maintenance.initialStatus')}:</span>
                  <MaintenanceStatusBadge status={entry.newStatus} />
                </>
              )}
            </div>
            <span className="text-xs text-muted-foreground" dir="ltr">
              {formatDate(entry.changedAt)}
            </span>
          </div>
          {entry.reason && (
            <p className="text-muted-foreground mt-1 bg-secondary/10 p-2 rounded">
              {t('maintenance.reason')}: {entry.reason}
            </p>
          )}
          <p className="text-xs text-muted-foreground mt-1">
            {t('maintenance.by')}: {actorName(entry.changedBy)}
          </p>
        </div>
      ))}
    </div>
  );
};
