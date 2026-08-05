import React from 'react';
import { useNavigate } from 'react-router';
import { PageHeader } from '@/shared/components/ui/Headers';
import { TenantForm } from '../components/TenantForm';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

export function CreateTenantPage() {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  return (
    <div className="container mx-auto py-8 px-4 max-w-4xl space-y-6">
      <PageHeader
        title={t('newTenant')}
        description={t('createTenantDesc')}
      />
      <TenantForm
        onSuccess={() => navigate('/tenants')}
        onCancel={() => navigate('/tenants')}
      />
    </div>
  );
}

export default CreateTenantPage;
