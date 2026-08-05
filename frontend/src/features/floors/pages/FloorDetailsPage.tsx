import React from 'react';
import { useNavigate, useParams } from 'react-router';
import { useFloor } from '../hooks/useFloors';
import { FloorDetails } from '../components/FloorDetails';
import { getFloorTranslation } from '../constants/translations';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useSetBreadcrumbTitle } from '@/shared/components/layout/BreadcrumbContext';

export default function FloorDetailsPage() {
  const navigate = useNavigate();
  const { buildingId, floorId } = useParams<{ buildingId?: string; floorId: string }>();
  const { language } = useTranslation();
  const t = (key: string) => getFloorTranslation(key, language);

  const { data: floor, isLoading, error } = useFloor(floorId!);
  const targetBuildingId = buildingId || floor?.buildingId;
  const { data: building } = useBuilding(targetBuildingId || '');

  useSetBreadcrumbTitle(floor?.id, floor?.floorLabel || (floor ? `Floor ${floor.floorNumber}` : undefined));

  if (isLoading) {
    return <div className="p-8 text-center">{t('loadingData')}</div>;
  }

  if (error || !floor) {
    return <div className="p-8 text-center text-destructive">{t('notFound')}</div>;
  }

  const buildingName = building?.name || 'Building';
  const displayLabel = floor.floorLabel || `Floor ${floor.floorNumber}`;

  return (
    <div className="container mx-auto py-8 px-4">
      <Button 
        variant="ghost" 
        className="mb-4 pl-0" 
        onClick={() => navigate(`/buildings/${targetBuildingId}`)}
      >
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {t('backToBuilding')} ({buildingName})
      </Button>

      <PageHeader 
        title={displayLabel} 
        description={`Building: ${buildingName}`}
      />

      <div className="mt-8">
        <FloorDetails floor={floor} />
      </div>
    </div>
  );
}
