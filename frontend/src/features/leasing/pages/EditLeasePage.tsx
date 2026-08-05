import React from 'react';
import { useParams, useNavigate } from 'react-router';
import { PageHeader } from '@/shared/components/ui/Headers';
import { LeaseContractForm } from '../components/LeaseContractForm';
import { useLeaseDetails, useUpdateDraftLease } from '../hooks/useLeasing';
import { CreateLeaseContractFormValues } from '../schemas/leasing.schema';
import { getLeasingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

import { Button } from '@/app/components/ui/button';
import { extractUserFriendlyError } from '@/shared/utils';

export function EditLeasePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);

  const { data: contract, isLoading, error, refetch } = useLeaseDetails(id || '');

  if (isLoading) {
    return <div className="container mx-auto py-8 px-4 text-center">{t('loadingLeaseDetails')}</div>;
  }

  if (error || !contract) {
    return (
      <div className="container mx-auto py-8 px-4 text-center space-y-4">
        <p className="text-destructive font-medium">
          {error ? extractUserFriendlyError(error, t('loadError')) : t('loadError')}
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
        title={`${t('editContract')} — ${contract.contractNumber}`}
        description={t('editContractDesc')}
      />
      <LeaseContractForm
        initialValues={contract}
        contractId={id}
        isEdit={true}
        onSuccess={() => navigate(`/leases/${id}`)}
        onCancel={() => navigate(`/leases/${id}`)}
      />
    </div>
  );
}

export default EditLeasePage;
