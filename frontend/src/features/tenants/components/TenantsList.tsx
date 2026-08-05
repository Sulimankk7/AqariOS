import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { PageHeader } from '@/shared/components/ui/Headers';
import { useSearchTenants } from '../hooks/useTenants';
import { TenantDto } from '../types/tenants.types';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { Plus, Eye, Edit, Trash2 } from 'lucide-react';
import { DeleteTenantDialog } from './DeleteTenantDialog';
import { extractUserFriendlyError } from '@/shared/utils';

export function TenantsList() {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  const [searchTerm, setSearchTerm] = useState('');
  const [deletingTenant, setDeletingTenant] = useState<{ id: string; name: string } | null>(null);

  const { data: tenants, isLoading, error, refetch } = useSearchTenants(searchTerm);

  const columns: Column<TenantDto>[] = [
    {
      key: 'name',
      header: t('name'),
      accessor: (row) => row.name,
      sortable: true,
      cell: (row) => (
        <div
          className="font-medium text-primary cursor-pointer hover:underline"
          onClick={() => navigate(`/tenants/${row.id}`)}
        >
          {row.name}
        </div>
      ),
    },
    {
      key: 'nationalId',
      header: t('nationalId'),
      accessor: (row) => row.nationalId,
      sortable: true,
    },
    {
      key: 'phone',
      header: t('phone'),
      accessor: (row) => row.phone,
      sortable: true,
    },
    {
      key: 'occupation',
      header: t('occupation'),
      accessor: (row) => row.occupation || '—',
    },
    {
      key: 'employer',
      header: t('employer'),
      accessor: (row) => row.employer || '—',
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
            title={t('viewDetails')}
            aria-label={t('viewDetails')}
            onClick={() => navigate(`/tenants/${row.id}`)}
          >
            <Eye className="w-4 h-4" />
          </Button>

          <Button
            variant="ghost"
            size="icon"
            title={t('edit')}
            aria-label={t('edit')}
            onClick={() => navigate(`/tenants/${row.id}/edit`)}
          >
            <Edit className="w-4 h-4" />
          </Button>

          <Button
            variant="ghost"
            size="icon"
            title={t('delete')}
            aria-label={t('delete')}
            className="text-destructive hover:text-destructive hover:bg-destructive/10"
            onClick={() => setDeletingTenant({ id: row.id, name: row.name })}
          >
            <Trash2 className="w-4 h-4" />
          </Button>
        </div>
      ),
    },
  ];

  if (error) {
    return (
      <div className="p-8 text-center space-y-4">
        <p className="text-destructive font-medium">
          {extractUserFriendlyError(error, t('loadError'))}
        </p>
        <Button variant="outline" onClick={() => refetch()}>
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
          <Button onClick={() => navigate('/tenants/new')}>
            <Plus className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('newTenant')}
          </Button>
        }
      />

      <DataTable
        data={tenants || []}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder={t('searchPlaceholder')}
        onSearchChange={setSearchTerm}
      />

      {/* Delete Modal */}
      <DeleteTenantDialog
        tenantId={deletingTenant?.id || null}
        tenantName={deletingTenant?.name}
        open={!!deletingTenant}
        onOpenChange={(open) => !open && setDeletingTenant(null)}
      />
    </div>
  );
}
