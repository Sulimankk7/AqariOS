import React, { useState } from 'react';
import { PageHeader } from '@/shared/components/ui/Headers';
import { Button } from '@/app/components/ui/button';
import { Plus } from 'lucide-react';
import { MaintenanceList } from '../components/MaintenanceList';
import { MaintenanceDrawer } from '../components/MaintenanceDrawer';
import { CreateMaintenanceRequestDialog } from '../components/CreateMaintenanceRequestDialog';
import { useMaintenanceRequests } from '../hooks/useMaintenance';
import { extractUserFriendlyError } from '@/shared/utils';
import { MaintenanceRequestSummaryDto } from '../types/maintenance.types';
import { useTranslation } from '@/shared/i18n';

export const MaintenancePage = () => {
  const { t } = useTranslation();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRequestId, setSelectedRequestId] = useState<string | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  // We are using the search text as the only filter for this simple list. 
  // LastSeenId pagination could be implemented here as well.
  const { data: requests, isLoading, error, refetch } = useMaintenanceRequests({
    searchText: searchQuery || undefined,
  });

  const handleRowClick = (row: MaintenanceRequestSummaryDto) => {
    setSelectedRequestId(row.id);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('maintenance.title')}
        description={t('maintenance.subtitle')}
        actions={
          <Button onClick={() => setIsCreateOpen(true)}>
            <Plus className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('maintenance.create')}
          </Button>
        }
      />

      {error ? (
        <div className="p-8 text-center space-y-4">
          <p className="text-destructive font-medium">
            {extractUserFriendlyError(error, t('maintenance.loadError'))}
          </p>
          <Button variant="outline" onClick={() => refetch()}>
            {t('maintenance.retry')}
          </Button>
        </div>
      ) : (
        <MaintenanceList 
          data={requests || []} 
          isLoading={isLoading} 
          onRowClick={handleRowClick}
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
        />
      )}

      {selectedRequestId && (
        <MaintenanceDrawer 
          requestId={selectedRequestId}
          open={!!selectedRequestId}
          onOpenChange={(open) => !open && setSelectedRequestId(null)}
        />
      )}

      <CreateMaintenanceRequestDialog 
        open={isCreateOpen}
        onOpenChange={setIsCreateOpen}
      />
    </div>
  );
};
