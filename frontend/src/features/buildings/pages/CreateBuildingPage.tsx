import React from 'react';
import { useNavigate } from 'react-router';
import { BuildingForm } from '../components/BuildingForm';
import { useCreateBuilding } from '../hooks/useBuildings';
import { BuildingFormValues } from '../schemas/buildings.schema';
import { getBuildingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/app/components/ui/button';

export default function CreateBuildingPage() {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);

  const createMutation = useCreateBuilding();

  const handleSubmit = (data: BuildingFormValues) => {
    createMutation.mutate(data, {
      onSuccess: () => navigate('/buildings')
    });
  };

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate('/buildings')}>
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {t('backToBuildings')}
      </Button>

      <PageHeader 
        title={t('addBuilding')} 
        description={t('createPageDescription')}
      />

      <div className="mt-8 bg-card rounded-lg border shadow-sm p-6">
        <BuildingForm onSubmit={handleSubmit} isLoading={createMutation.isPending} />
      </div>
    </div>
  );
}
