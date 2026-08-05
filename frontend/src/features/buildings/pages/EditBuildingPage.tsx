import React from 'react';
import { useNavigate, useParams } from 'react-router';
import { BuildingForm } from '../components/BuildingForm';
import { useBuilding, useUpdateBuilding } from '../hooks/useBuildings';
import { BuildingFormValues } from '../schemas/buildings.schema';
import { getBuildingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useSetBreadcrumbTitle } from '@/shared/components/layout/BreadcrumbContext';

export default function EditBuildingPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);
  
  const { data: building, isLoading, error } = useBuilding(id!);
  const updateMutation = useUpdateBuilding();

  useSetBreadcrumbTitle(building?.id, building?.name);

  const handleSubmit = (data: BuildingFormValues) => {
    updateMutation.mutate(
      { id: id!, data },
      { onSuccess: () => navigate('/buildings') }
    );
  };

  if (isLoading) {
    return <div className="p-8 text-center">{t('loadingData')}</div>;
  }

  if (error || !building) {
    return <div className="p-8 text-center text-destructive">{t('notFound')}</div>;
  }

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate('/buildings')}>
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {t('backToBuildings')}
      </Button>

      <PageHeader 
        title={t('editBuilding')} 
        description={t('editPageDescription')}
      />

      <div className="mt-8 bg-card rounded-lg border shadow-sm p-6">
        <BuildingForm 
          initialData={building} 
          onSubmit={handleSubmit} 
          isLoading={updateMutation.isPending} 
        />
      </div>
    </div>
  );
}
