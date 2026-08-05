import React from 'react';
import { useParams } from 'react-router';
import { Button } from '@/app/components/ui/button';
import { TenantDetails } from '../components/TenantDetails';
import { useTenantDetails } from '../hooks/useTenants';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';

export function TenantDetailsPage() {
  const { id } = useParams<{ id: string }>();
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
    <div className="container mx-auto py-8 px-4">
      <TenantDetails tenant={tenant} />
    </div>
  );
}

export default TenantDetailsPage;
