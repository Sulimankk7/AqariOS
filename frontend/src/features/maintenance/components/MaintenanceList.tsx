import React from 'react';
import { MaintenanceRequestSummaryDto, MaintenanceStatus } from '../types/maintenance.types';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { MaintenanceStatusBadge, MaintenancePriorityBadge } from './MaintenanceStatusBadge';
import { Eye } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useTranslation } from '@/shared/i18n';
import { maintenanceCategoryLabel } from './MaintenanceStatusBadge';
import { useBuildings } from '@/features/buildings/hooks/useBuildings';
import { useApartments } from '@/features/apartments/hooks/useApartments';

interface MaintenanceListProps {
  data: MaintenanceRequestSummaryDto[];
  isLoading: boolean;
  onRowClick: (row: MaintenanceRequestSummaryDto) => void;
  onSearchChange: (val: string) => void;
  searchQuery: string;
}

export const MaintenanceList = ({
  data,
  isLoading,
  onRowClick,
  onSearchChange,
  searchQuery,
}: MaintenanceListProps) => {
  const { t } = useTranslation();
  const { data: buildings } = useBuildings();
  const { data: apartments } = useApartments();
  const columns: Column<MaintenanceRequestSummaryDto>[] = [
    {
      key: 'title',
      header: t('maintenance.title'),
      accessor: (row) => row.title,
      cell: (row) => (
        <div className="font-medium text-foreground cursor-pointer hover:underline" onClick={() => onRowClick(row)}>
          {row.title}
        </div>
      )
    },
    {
      key: 'location',
      header: t('maintenance.location'),
      cell: (row) => (
        <div className="text-sm">
          {buildings?.find((item) => item.id === row.buildingId)?.name || '—'}
          {row.apartmentId && ` - ${t('maintenance.apartment')} ${apartments?.find((item) => item.id === row.apartmentId)?.unitNumber || '—'}`}
        </div>
      )
    },
    {
      key: 'category',
      header: t('maintenance.category'),
      cell: (row) => maintenanceCategoryLabel(row.category, t),
    },
    {
      key: 'priority',
      header: t('maintenance.priority'),
      cell: (row) => <MaintenancePriorityBadge priority={row.priority} />
    },
    {
      key: 'status',
      header: t('maintenance.status'),
      cell: (row) => <MaintenanceStatusBadge status={row.status} />
    },
    {
      key: 'requestDate',
      header: t('maintenance.requestDate'),
      cell: (row) => <div dir="ltr" className="text-right">{row.requestDate}</div>
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      cell: (row) => (
        <div className="flex justify-end gap-1">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => onRowClick(row)}
          >
            <Eye className="w-4 h-4" />
          </Button>
        </div>
      ),
    }
  ];

  return (
    <DataTable
      data={data}
      columns={columns}
      isLoading={isLoading}
      searchable
      searchPlaceholder={t('maintenance.search')}
      searchConfig={{
        searchTerm: searchQuery,
        onChange: onSearchChange,
        placeholder: t('maintenance.search')
      }}
    />
  );
};
