import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { Tabs, TabsList, TabsTrigger } from '@/app/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/app/components/ui/select';
import { PageHeader } from '@/shared/components/ui/Headers';
import { useSearchLeases, useExpiringLeases } from '../hooks/useLeasing';
import { LeaseContractDto, ContractStatus } from '../types/leasing.types';
import { getLeasingTranslation } from '../constants/translations';
import {
  contractStatusToLabel,
  contractStatusToBadgeVariant,
  paymentFrequencyToLabel,
} from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import {
  Plus,
  Eye,
  Edit,
  CheckCircle,
  RefreshCw,
  XCircle,
  FilePlus,
  Clock,
} from 'lucide-react';
import { ActivateLeaseDialog } from './ActivateLeaseDialog';
import { AttachDocumentDialog } from './AttachDocumentDialog';
import { TerminateLeaseDialog } from './TerminateLeaseDialog';
import { RenewLeaseDialog } from './RenewLeaseDialog';
import { extractUserFriendlyError } from '@/shared/utils';

export function LeaseContractsList() {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);

  const [activeTab, setActiveTab] = useState<'all' | 'expiring'>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [daysAhead, setDaysAhead] = useState<number>(30);

  const { data: allContracts, isLoading: isLoadingAll, error: errorAll, refetch: refetchAll } = useSearchLeases(
    searchTerm,
    50
  );
  const { data: expiringContracts, isLoading: isLoadingExpiring, error: errorExpiring, refetch: refetchExpiring } =
    useExpiringLeases(daysAhead);

  // Dialog state
  const [activateContract, setActivateContract] = useState<{ id: string; number: string } | null>(
    null
  );
  const [attachContractId, setAttachContractId] = useState<string | null>(null);
  const [terminateContractId, setTerminateContractId] = useState<string | null>(null);
  const [renewContract, setRenewContract] = useState<LeaseContractDto | null>(null);

  const data = activeTab === 'all' ? allContracts : expiringContracts;
  const isLoading = activeTab === 'all' ? isLoadingAll : isLoadingExpiring;
  const error = activeTab === 'all' ? errorAll : errorExpiring;

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
      key: 'startDate',
      header: t('startDate'),
      accessor: (row) => row.startDate,
      sortable: true,
    },
    {
      key: 'endDate',
      header: t('endDate'),
      accessor: (row) => row.endDate,
      sortable: true,
    },
    {
      key: 'monthlyRent',
      header: t('monthlyRent'),
      accessor: (row) => `${row.monthlyRentAmount} ${row.currency || 'JOD'}`,
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
      cell: (row) => {
        const isDraftOrPending =
          row.status === ContractStatus.Draft ||
          row.status === ContractStatus.PendingSignature;
        const isActive = row.status === ContractStatus.Active;
        const isExpired = row.status === ContractStatus.Expired;
        const isTerminated = row.status === ContractStatus.Terminated;

        return (
          <div className="flex justify-end gap-1">
            <Button
              variant="ghost"
              size="icon"
              title={t('viewDetails')}
              aria-label={t('viewDetails')}
              onClick={() => navigate(`/leases/${row.id}`)}
            >
              <Eye className="w-4 h-4" />
            </Button>

            {isDraftOrPending && (
              <>
                <Button
                  variant="ghost"
                  size="icon"
                  title={t('editDraft')}
                  aria-label={t('editDraft')}
                  onClick={() => navigate(`/leases/${row.id}/edit`)}
                >
                  <Edit className="w-4 h-4" />
                </Button>
                {/* Inline Activate Action for Draft contracts */}
                <Button
                  variant="ghost"
                  size="icon"
                  title={t('activate')}
                  aria-label={t('activate')}
                  className="text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50"
                  onClick={() => setActivateContract({ id: row.id, number: row.contractNumber })}
                >
                  <CheckCircle className="w-4 h-4" />
                </Button>
              </>
            )}

            {(isActive || isExpired) && (
              <Button
                variant="ghost"
                size="icon"
                title={t('renew')}
                aria-label={t('renew')}
                onClick={() => setRenewContract(row)}
              >
                <RefreshCw className="w-4 h-4" />
              </Button>
            )}

            {isActive && (
              <Button
                variant="ghost"
                size="icon"
                title={t('terminate')}
                aria-label={t('terminate')}
                className="text-destructive hover:text-destructive hover:bg-destructive/10"
                onClick={() => setTerminateContractId(row.id)}
              >
                <XCircle className="w-4 h-4" />
              </Button>
            )}

            {!isTerminated && (
              <Button
                variant="ghost"
                size="icon"
                title={t('attachDoc')}
                aria-label={t('attachDoc')}
                onClick={() => setAttachContractId(row.id)}
              >
                <FilePlus className="w-4 h-4" />
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  if (error) {
    const refetchFn = activeTab === 'all' ? refetchAll : refetchExpiring;
    return (
      <div className="p-8 text-center space-y-4">
        <p className="text-destructive font-medium">
          {extractUserFriendlyError(error, t('loadError'))}
        </p>
        <Button variant="outline" onClick={() => refetchFn()}>
          {t('retry')}
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('pageTitle')}
        description={t('pageDescription')}
        actions={
          <Button onClick={() => navigate('/leases/new')}>
            <Plus className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('newContract')}
          </Button>
        }
      />

      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <Tabs
          value={activeTab}
          onValueChange={(val) => setActiveTab(val as 'all' | 'expiring')}
          className="w-full sm:w-auto"
        >
          <TabsList>
            <TabsTrigger value="all">{t('allContracts')}</TabsTrigger>
            <TabsTrigger value="expiring">
              {t('expiringContracts')} ({daysAhead} {t('days')})
            </TabsTrigger>
          </TabsList>
        </Tabs>

        {activeTab === 'expiring' && (
          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4 text-muted-foreground" />
            <span className="text-xs text-muted-foreground whitespace-nowrap">{t('daysAhead')}:</span>
            <Select
              value={daysAhead.toString()}
              onValueChange={(val) => setDaysAhead(Number(val))}
            >
              <SelectTrigger className="w-[120px] h-8 text-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="7">7 {t('days')}</SelectItem>
                <SelectItem value="14">14 {t('days')}</SelectItem>
                <SelectItem value="30">30 {t('days')}</SelectItem>
                <SelectItem value="60">60 {t('days')}</SelectItem>
                <SelectItem value="90">90 {t('days')}</SelectItem>
                <SelectItem value="120">120 {t('days')}</SelectItem>
              </SelectContent>
            </Select>
          </div>
        )}
      </div>

      <DataTable
        data={data || []}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder={t('searchPlaceholder')}
        onSearchChange={activeTab === 'all' ? setSearchTerm : undefined}
      />

      {/* Modals */}
      <ActivateLeaseDialog
        contractId={activateContract?.id || null}
        contractNumber={activateContract?.number}
        open={!!activateContract}
        onOpenChange={(open) => !open && setActivateContract(null)}
      />

      <AttachDocumentDialog
        contractId={attachContractId}
        open={!!attachContractId}
        onOpenChange={(open) => !open && setAttachContractId(null)}
      />

      <TerminateLeaseDialog
        contractId={terminateContractId}
        open={!!terminateContractId}
        onOpenChange={(open) => !open && setTerminateContractId(null)}
      />

      <RenewLeaseDialog
        contract={renewContract}
        open={!!renewContract}
        onOpenChange={(open) => !open && setRenewContract(null)}
      />
    </div>
  );
}
