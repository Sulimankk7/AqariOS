import React from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { ApartmentForm } from '../components/ApartmentForm';
import { useCreateApartment } from '../hooks/useApartments';
import { ApartmentFormValues } from '../schemas/apartments.schema';
import { getApartmentTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft, AlertCircle } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { extractUserFriendlyError, getUserFacingErrorKind } from '@/shared/utils';

export default function CreateApartmentPage() {
  const navigate = useNavigate();
  const params = useParams<{ floorId?: string }>();
  const [searchParams] = useSearchParams();
  const { language } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);

  const floorId = params.floorId || searchParams.get('floorId') || '';
  const buildingId = searchParams.get('buildingId') || '';
  const { data: building, isLoading: isLoadingBuilding, error: buildingError, refetch: refetchBuilding } = useBuilding(buildingId);

  const createMutation = useCreateApartment();

  const handleSubmit = (data: ApartmentFormValues) => {
    const targetFloorId = data.floorId || floorId;
    if (!targetFloorId) {
      return;
    }
    createMutation.mutate(
      { floorId: targetFloorId, data },
      { onSuccess: () => navigate(buildingId ? `/buildings/${buildingId}` : '/apartments') }
    );
  };

  if (buildingId && isLoadingBuilding) {
    return <div className="p-8 text-center">{t('loadingData')}</div>;
  }

  if (buildingId && (buildingError || !building)) {
    const isNotFound = !building && (!buildingError || getUserFacingErrorKind(buildingError) === 'notFound');
    return <div className="p-8 text-center space-y-4"><p className="text-destructive">{isNotFound ? t('notFound') : extractUserFriendlyError(buildingError, t('loadError'))}</p>{!isNotFound && <Button variant="outline" onClick={() => refetchBuilding()}>{t('retry')}</Button>}</div>;
  }

  const backPath = buildingId ? `/buildings/${buildingId}` : '/apartments';

  return (
    <div className="container mx-auto py-8 px-4">
      <Button variant="ghost" className="mb-4 pl-0" onClick={() => navigate(backPath)}>
        <ArrowLeft className="mr-2 h-4 w-4 rtl:rotate-180" />
        {building ? building.name : t('backToApartments')}
      </Button>

      <PageHeader 
        title={t('addApartment')} 
        description={t('createPageDescription')}
      />

      {!floorId && !buildingId && (
        <div className="mt-4 p-4 border border-amber-500/30 bg-amber-500/10 rounded-lg flex items-center gap-3 text-amber-600 dark:text-amber-400 text-sm">
          <AlertCircle className="h-5 w-5 shrink-0" />
          <span>{t('floorRequiredMessage')}</span>
        </div>
      )}

      <div className="mt-6 bg-card rounded-lg border shadow-sm p-6">
        <ApartmentForm 
          defaultFloorId={floorId}
          buildingId={buildingId || undefined}
          buildingName={building?.name}
          onSubmit={handleSubmit} 
          isLoading={createMutation.isPending} 
        />
      </div>
    </div>
  );
}
