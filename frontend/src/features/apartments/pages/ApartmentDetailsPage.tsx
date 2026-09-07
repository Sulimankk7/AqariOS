import React from 'react';
import { useNavigate, useParams } from 'react-router';
import { useApartment } from '../hooks/useApartments';
import { ApartmentDetails } from '../components/ApartmentDetails';
import { getApartmentTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft, Edit } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useSetBreadcrumbTitle } from '@/shared/components/layout/BreadcrumbContext';
import { extractUserFriendlyError, getUserFacingErrorKind } from '@/shared/utils';

export default function ApartmentDetailsPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { language } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);
  
  const { data: apartment, isLoading, error, refetch } = useApartment(id!);

  useSetBreadcrumbTitle(apartment?.id, apartment?.unitNumber ? `Unit ${apartment.unitNumber}` : undefined);

  if (isLoading) {
    return <div className="p-8 text-center">{t('loadingData')}</div>;
  }

  if (error || !apartment) {
    const isNotFound = !apartment && (!error || getUserFacingErrorKind(error) === 'notFound');
    return <div className="p-8 text-center space-y-4"><p className="text-destructive">{isNotFound ? t('notFound') : extractUserFriendlyError(error, t('loadError'))}</p>{!isNotFound && <Button variant="outline" onClick={() => refetch()}>{t('retry')}</Button>}</div>;
  }

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate('/apartments')}>
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {t('backToApartments')}
      </Button>

      <PageHeader 
        title={`${t('unitNumber')} ${apartment.unitNumber}`} 
        actions={
          <Button onClick={() => navigate(`/apartments/${apartment.id}/edit`)}>
            <Edit className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('edit')}
          </Button>
        }
      />

      <div className="mt-8">
        <ApartmentDetails apartment={apartment} />
      </div>
    </div>
  );
}
