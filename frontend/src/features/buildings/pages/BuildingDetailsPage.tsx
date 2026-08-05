import React from 'react';
import { useNavigate, useParams } from 'react-router';
import { useBuilding } from '../hooks/useBuildings';
import { BuildingDetails } from '../components/BuildingDetails';
import { getBuildingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArrowLeft, Edit } from 'lucide-react';
import { Button } from '@/app/components/ui/button';
import { useSetBreadcrumbTitle } from '@/shared/components/layout/BreadcrumbContext';

export default function BuildingDetailsPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);
  
  const { data: building, isLoading, error } = useBuilding(id!);

  useSetBreadcrumbTitle(building?.id, building?.name);

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
        title={building.name} 
        description={`${t('internalCode')}: ${building.internalCode || '-'}`}
        actions={
          <Button onClick={() => navigate(`/buildings/${building.id}/edit`)}>
            <Edit className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('edit')}
          </Button>
        }
      />

      <div className="mt-8">
        <BuildingDetails building={building} />
      </div>
    </div>
  );
}
