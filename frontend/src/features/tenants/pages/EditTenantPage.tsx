import React from 'react';
import { useParams, useNavigate } from 'react-router';
import { PageHeader } from '@/shared/components/ui/Headers';
import { Button } from '@/app/components/ui/button';
import { TenantForm } from '../components/TenantForm';
import { useTenantDetails } from '../hooks/useTenants';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';

export function EditTenantPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  const { data: tenant, isLoading, error, refetch } = useTenantDetails(id || '');

  if (isLoading) {
    return <div className="container mx-auto py-8 px-4 text-center">{t('loading')}</div>;
  }

  if (error || !tenant) {
    return (
      <div className="container mx-auto py-8 px-4 text-center space-y-4">
        <p className="text-destructive font-medium">
          {error ? extractUserFriendlyError(error, t('loadError')) : t('notFound')}
        </p>
        <Button variant="outline" onClick={() => refetch()}>
          {t('retry')}
        </Button>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 px-4 max-w-4xl space-y-6">
      <PageHeader
        title={`${t('editTenant')} — ${tenant.name}`}
        description={t('editTenantDesc')}
      />
      <TenantForm
        initialValues={tenant}
        tenantId={tenant.id}
        isEdit
        onSuccess={() => navigate(`/tenants/${tenant.id}`)}
        onCancel={() => navigate(`/tenants/${tenant.id}`)}
      />
    </div>
  );
}

export default EditTenantPage;
