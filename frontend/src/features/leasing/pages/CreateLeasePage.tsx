import React from 'react';
import { useNavigate } from 'react-router';
import { PageHeader } from '@/shared/components/ui/Headers';
import { LeaseContractForm } from '../components/LeaseContractForm';
import { useCreateLease } from '../hooks/useLeasing';
import { CreateLeaseContractFormValues } from '../schemas/leasing.schema';
import { getLeasingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

export function CreateLeasePage() {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);

  return (
    <div className="container mx-auto py-8 px-4 max-w-4xl space-y-6">
      <PageHeader
        title={t('newContract')}
        description={t('newContractDesc')}
      />
      <LeaseContractForm
        onSuccess={() => navigate('/leases')}
        onCancel={() => navigate('/leases')}
      />
    </div>
  );
}

export default CreateLeasePage;
