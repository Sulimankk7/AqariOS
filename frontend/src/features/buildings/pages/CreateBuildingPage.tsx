import React from 'react';
import { useNavigate } from 'react-router';
import { BuildingForm } from '../components/BuildingForm';
import { useCreateBuilding } from '../hooks/useBuildings';
import { BuildingFormValues } from '../schemas/buildings.schema';
import { buildingTranslations as t } from '../constants/translations';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/app/components/ui/button';

export default function CreateBuildingPage() {
  const navigate = useNavigate();
  const createMutation = useCreateBuilding();

  const handleSubmit = (data: BuildingFormValues) => {
    createMutation.mutate(data, {
      onSuccess: () => navigate('/properties/buildings')
    });
  };

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate('/properties/buildings')}>
        <ArrowLeft className="mr-2 h-4 w-4" />
        {t.backToBuildings}
      </Button>

      <PageHeader 
        title={t.addBuilding} 
        description="Add a new building to your property portfolio."
      />

      <div className="mt-8 bg-card rounded-lg border shadow-sm p-6">
        <BuildingForm onSubmit={handleSubmit} isLoading={createMutation.isPending} />
      </div>
    </div>
  );
}
