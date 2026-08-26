import React from 'react';
import { X } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { Skeleton } from '@/app/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/app/components/ui/tabs';
import { ErrorState } from '@/shared/components/ui/Feedback';
import { useTranslation } from '@/shared/i18n';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { useApartment } from '@/features/apartments/hooks/useApartments';
import { useTenantDetails } from '@/features/tenants/hooks/useTenants';
import { MaintenanceRequestDetailDto, MaintenanceStatus } from '../types/maintenance.types';
import { useMaintenanceRequest, useUpdateMaintenanceStatus } from '../hooks/useMaintenance';
import { MaintenanceStatusBadge, MaintenancePriorityBadge, maintenanceCategoryLabel } from './MaintenanceStatusBadge';
import { MaintenanceCommentsTab } from './MaintenanceCommentsTab';
import { MaintenanceAttachmentsTab } from './MaintenanceAttachmentsTab';
import { MaintenanceHistoryTab } from './MaintenanceHistoryTab';

interface Props { requestId: string | null; open: boolean; onOpenChange: (open: boolean) => void }

const transitions: Record<MaintenanceStatus, MaintenanceStatus[]> = {
  [MaintenanceStatus.Open]: [MaintenanceStatus.InProgress, MaintenanceStatus.Waiting, MaintenanceStatus.Cancelled],
  [MaintenanceStatus.InProgress]: [MaintenanceStatus.Waiting, MaintenanceStatus.Resolved, MaintenanceStatus.Cancelled],
  [MaintenanceStatus.Waiting]: [MaintenanceStatus.InProgress, MaintenanceStatus.Resolved, MaintenanceStatus.Cancelled],
  [MaintenanceStatus.Resolved]: [MaintenanceStatus.Closed, MaintenanceStatus.InProgress],
  [MaintenanceStatus.Closed]: [], [MaintenanceStatus.Cancelled]: [],
};

export const MaintenanceDrawer = ({ requestId, open, onOpenChange }: Props) => {
  const { t, formatDate } = useTranslation();
  const query = useMaintenanceRequest(requestId || '', !!requestId && open);
  const request = query.data;
  const building = useBuilding(request?.buildingId || '');
  const apartment = useApartment(request?.apartmentId || '');
  const tenant = useTenantDetails(request?.tenantId || '');
  const updateStatus = useUpdateMaintenanceStatus(requestId);
  if (!open || !requestId) return null;

  return <>
    <div className="fixed inset-0 z-50 flex justify-end overflow-hidden bg-black/40 backdrop-blur-xs" onMouseDown={(event) => event.target === event.currentTarget && onOpenChange(false)}>
      <aside className="flex h-full w-full flex-col overflow-hidden border-s border-border bg-card shadow-2xl sm:max-w-[540px]" aria-label={t('maintenance.details')}>
        <header className="flex items-center justify-between gap-3 border-b border-border px-4 py-3">
          <h2 className="truncate text-base font-bold">{t('maintenance.details')}</h2>
          <button type="button" onClick={() => onOpenChange(false)} className="shrink-0 rounded-full p-2 text-muted-foreground hover:bg-secondary hover:text-foreground" aria-label={t('common.close')}><X className="h-5 w-5" /></button>
        </header>
        <div className="flex-1 overflow-y-auto overflow-x-hidden p-4">
          {query.isLoading && <div className="space-y-3"><Skeleton className="h-20 w-full" /><Skeleton className="h-28 w-full" /></div>}
          {query.isError && <ErrorState title={t('maintenance.loadError')} onRetry={() => query.refetch()} />}
          {request && <div className="space-y-5">
            <div><h3 className="text-lg font-bold leading-7">{request.title}</h3><div className="mt-2 flex flex-wrap gap-2"><MaintenanceStatusBadge status={request.status} /><MaintenancePriorityBadge priority={request.priority} /></div></div>
            <div className="grid grid-cols-2 gap-x-5 gap-y-3 text-xs">
              <Info label={t('maintenance.requestDate')} value={formatDate(request.requestDate)} mono />
              <Info label={t('maintenance.category')} value={maintenanceCategoryLabel(request.category, t)} />
              <Info label={t('maintenance.building')} value={building.data?.name || '—'} />
              {request.apartmentId && <Info label={t('maintenance.apartment')} value={(apartment.data as any)?.unitNumber || '—'} mono />}
              {request.tenantId && <Info label={t('maintenance.tenant')} value={(tenant.data as any)?.name || '—'} />}
            </div>
            <section><h4 className="text-xs font-semibold">{t('maintenance.description')}</h4><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-foreground">{request.description}</p></section>
            {request.internalNotes && <section><h4 className="text-xs font-semibold">{t('maintenance.internalNotes')}</h4><p className="mt-2 rounded-md bg-secondary/40 px-3 py-2 text-sm whitespace-pre-wrap">{request.internalNotes}</p></section>}
            <StatusActions
              request={request}
              onSelect={(status) => updateStatus.mutate({ newStatus: status })}
              updating={updateStatus.isPending}
              t={t}
            />
            <Tabs defaultValue="comments">
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="comments">{t('maintenance.comments')} ({request.commentCount})</TabsTrigger>
                <TabsTrigger value="attachments">{t('maintenance.attachments')} ({request.attachmentCount})</TabsTrigger>
                <TabsTrigger value="history">{t('maintenance.history')}</TabsTrigger>
              </TabsList>
              <TabsContent value="comments" className="m-0"><MaintenanceCommentsTab requestId={request.id} /></TabsContent>
              <TabsContent value="attachments" className="m-0"><MaintenanceAttachmentsTab requestId={request.id} /></TabsContent>
              <TabsContent value="history" className="m-0"><MaintenanceHistoryTab requestId={request.id} /></TabsContent>
            </Tabs>
          </div>}
        </div>
      </aside>
    </div>
  </>;
};

function Info({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) { return <div className="min-w-0"><span className="block text-muted-foreground">{label}</span><bdi dir={mono ? 'ltr' : undefined} className={`mt-1 block truncate font-medium text-foreground ${mono ? 'font-mono' : ''}`}>{value}</bdi></div>; }
function StatusActions({ request, onSelect, updating, t }: { request: MaintenanceRequestDetailDto; onSelect: (s: MaintenanceStatus) => void; updating: boolean; t: (key: string) => string }) {
  const labels: Record<MaintenanceStatus, string> = {
    [MaintenanceStatus.Open]: '', [MaintenanceStatus.InProgress]: request.status === MaintenanceStatus.Open ? t('maintenance.actions.start') : request.status === MaintenanceStatus.Resolved ? t('maintenance.actions.reopen') : t('maintenance.actions.resume'),
    [MaintenanceStatus.Waiting]: t('maintenance.actions.wait'), [MaintenanceStatus.Resolved]: t('maintenance.actions.resolve'), [MaintenanceStatus.Closed]: t('maintenance.actions.close'), [MaintenanceStatus.Cancelled]: t('maintenance.actions.cancel'),
  };
  const next = transitions[request.status];
  if (!next.length) return null;
  return <div className="flex flex-wrap gap-2 border-y border-border py-3">{next.map((status, index) => <Button key={status} size="sm" variant={status === MaintenanceStatus.Cancelled ? 'ghost' : index === 0 ? 'default' : 'outline'} className={status === MaintenanceStatus.Cancelled ? 'text-destructive hover:bg-destructive/10' : ''} onClick={() => onSelect(status)} disabled={updating}>{labels[status]}</Button>)}</div>;
}
