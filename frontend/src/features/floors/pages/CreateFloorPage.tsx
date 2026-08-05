import React from 'react';
import { useNavigate, useParams } from 'react-router';
import { FloorForm } from '../components/FloorForm';
import { useCreateFloor } from '../hooks/useFloors';
import { FloorFormValues } from '../schemas/floors.schema';
import { getFloorTranslation } from '../constants/translations';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft, Building2 } from 'lucide-react';
import { Button } from '@/app/components/ui/button';

export default function CreateFloorPage() {
  const navigate = useNavigate();
  const { buildingId } = useParams<{ buildingId: string }>();
  const { language } = useTranslation();
  const t = (key: string) => getFloorTranslation(key, language);

  const { data: building } = useBuilding(buildingId || '');
  const createMutation = useCreateFloor();

  const handleSubmit = (data: FloorFormValues) => {
    if (!buildingId) return;
    createMutation.mutate(
      { buildingId, data },
      { onSuccess: () => navigate(`/buildings/${buildingId}`) }
    );
  };

  const buildingName = building?.name || 'Building';

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate(`/buildings/${buildingId}`)}>
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {t('backToBuilding')} ({buildingName})
      </Button>

      <PageHeader 
        title={t('addFloor')} 
        description={`${t('createPageDescription')} (${buildingName})`}
      />

      <div className="mt-8 bg-card rounded-lg border shadow-sm p-6">
        <FloorForm onSubmit={handleSubmit} isLoading={createMutation.isPending} />
      </div>
    </div>
  );
}
