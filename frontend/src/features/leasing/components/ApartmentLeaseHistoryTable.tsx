import React from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Badge } from '@/app/components/ui/badge';
import { Button } from '@/app/components/ui/button';
import { useApartmentLeaseHistory } from '../hooks/useLeasing';
import { LeaseContractDto } from '../types/leasing.types';
import { getLeasingTranslation } from '../constants/translations';
import {
  contractStatusToLabel,
  contractStatusToBadgeVariant,
  paymentFrequencyToLabel,
} from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import { Eye } from 'lucide-react';

import { extractUserFriendlyError } from '@/shared/utils';

interface ApartmentLeaseHistoryTableProps {
  apartmentId: string;
  pageSize?: number;
}

export function ApartmentLeaseHistoryTable({
  apartmentId,
  pageSize = 50,
}: ApartmentLeaseHistoryTableProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);

  const { data: history, isLoading, error, refetch } = useApartmentLeaseHistory(apartmentId, pageSize);

  // ... columns ...
  const columns: Column<LeaseContractDto>[] = [
    {
      key: 'contractNumber',
      header: t('contractNumber'),
      accessor: (row) => row.contractNumber,
      sortable: true,
      cell: (row) => (
        <div
          className="font-medium text-primary cursor-pointer hover:underline"
          onClick={() => navigate(`/leases/${row.id}`)}
        >
          {row.contractNumber}
        </div>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      accessor: (row) => contractStatusToLabel(row.status, t),
      sortable: true,
      cell: (row) => (
        <Badge variant={contractStatusToBadgeVariant(row.status)}>
          {contractStatusToLabel(row.status, t)}
        </Badge>
      ),
    },
    {
      key: 'dates',
      header: `${t('startDate')} - ${t('endDate')}`,
      accessor: (row) => `${row.startDate} ~ ${row.endDate}`,
    },
    {
      key: 'monthlyRent',
      header: t('monthlyRent'),
      accessor: (row) => `${row.monthlyRentAmount} ${row.currency}`,
      sortable: true,
    },
    {
      key: 'frequency',
      header: t('paymentFrequency'),
      accessor: (row) => paymentFrequencyToLabel(row.paymentFrequency, t),
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      cell: (row) => (
        <Button
          variant="ghost"
          size="icon"
          title={t('viewDetails')}
          aria-label={t('viewDetails')}
          onClick={() => navigate(`/leases/${row.id}`)}
        >
          <Eye className="w-4 h-4" />
        </Button>
      ),
    },
  ];

  if (error) {
    return (
      <div className="p-4 text-center space-y-2">
        <p className="text-destructive font-medium">{extractUserFriendlyError(error, t('loadError'))}</p>
        <Button variant="outline" size="sm" onClick={() => refetch()}>
          {t('retry')}
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <DataTable
        data={history || []}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder={t('searchPlaceholder')}
      />
    </div>
  );
}
