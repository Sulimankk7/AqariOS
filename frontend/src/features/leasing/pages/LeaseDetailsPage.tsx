import React from 'react';
import { useParams, useNavigate } from 'react-router';
import { Button } from '@/app/components/ui/button';
import { PageHeader } from '@/shared/components/ui/Headers';
import { LeaseContractDetails } from '../components/LeaseContractDetails';
import { useLeaseDetails } from '../hooks/useLeasing';
import { getLeasingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { ArrowLeft, ArrowRight } from 'lucide-react';

import { extractUserFriendlyError } from '@/shared/utils';

export function LeaseDetailsPage() {
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

  const BackIcon = language === 'ar' ? ArrowRight : ArrowLeft;

  return (
    <div className="container mx-auto py-8 px-4 space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => navigate('/leases')}>
          <BackIcon className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
          {t('allContracts')}
        </Button>
      </div>

      <LeaseContractDetails contract={contract} />
    </div>
  );
}

export default LeaseDetailsPage;
