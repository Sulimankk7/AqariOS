import React from 'react';
import { useNavigate, useParams } from 'react-router';
import { ApartmentForm } from '../components/ApartmentForm';
import { useApartment, useUpdateApartment } from '../hooks/useApartments';
import { ApartmentFormValues } from '../schemas/apartments.schema';
import { getApartmentTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useSetBreadcrumbTitle } from '@/shared/components/layout/BreadcrumbContext';

export default function EditApartmentPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { language } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);
  
  const { data: apartment, isLoading, error } = useApartment(id!);
  const updateMutation = useUpdateApartment();

  useSetBreadcrumbTitle(apartment?.id, apartment?.unitNumber ? `Unit ${apartment.unitNumber}` : undefined);

  const handleSubmit = (data: ApartmentFormValues) => {
    updateMutation.mutate(
      { id: id!, data },
      { onSuccess: () => navigate('/apartments') }
    );
  };

  if (isLoading) {
    return <div className="p-8 text-center">{t('loadingData')}</div>;
  }

  if (error || !apartment) {
    return <div className="p-8 text-center text-destructive">{t('notFound')}</div>;
  }

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate('/apartments')}>
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {t('backToApartments')}
      </Button>

      <PageHeader 
        title={`${t('editApartment')}: ${apartment.unitNumber}`} 
        description={t('editPageDescription')}
      />

      <div className="mt-8 bg-card rounded-lg border shadow-sm p-6">
        <ApartmentForm 
          initialData={apartment} 
          isEditMode={true}
          onSubmit={handleSubmit} 
          isLoading={updateMutation.isPending} 
        />
      </div>
    </div>
  );
}
