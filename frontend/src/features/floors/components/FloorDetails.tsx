import React from 'react';
import { Link, useNavigate } from 'react-router';
import { FloorDto } from '../types/floors.types';
import { getFloorTranslation } from '../constants/translations';
import { floorTypeToLabel } from '../constants/floorEnums';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { useApartments } from '@/features/apartments/hooks/useApartments';
import { ApartmentsList } from '@/features/apartments/components/ApartmentsList';
import { useTranslation } from '@/shared/i18n';
import { Badge } from '@/app/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { 
  Layers, 
  Building2, 
  Home, 
  Plus, 
  Edit 
} from 'lucide-react';

interface FloorDetailsProps {
  floor: FloorDto;
}

export function FloorDetails({ floor }: FloorDetailsProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getFloorTranslation(key, language);

  const { data: building } = useBuilding(floor.buildingId);
  const { data: apartments } = useApartments({ floorId: floor.id });

  const buildingName = building?.name || 'Building Details';
  const displayLabel = floor.floorLabel || `Floor ${floor.floorNumber}`;
  const apartmentCount = apartments ? apartments.length : floor.apartmentsCount;

  return (
    <div className="space-y-6 max-w-6xl">
      {/* Header Card */}
      <Card>
        <CardHeader>
          <div className="flex justify-between items-start">
            <div>
              <CardTitle className="text-2xl font-bold flex items-center gap-2 text-foreground">
                <Layers className="h-6 w-6 text-primary shrink-0" />
                {displayLabel}
              </CardTitle>
              <CardDescription className="mt-2 flex items-center gap-4 text-xs text-muted-foreground">
                {/* Clickable Building Link */}
                <div className="flex items-center gap-1.5" title={`${t('building')}: ${buildingName}`}>
                  <Building2 className="h-3.5 w-3.5 text-primary" />
                  <span>{t('building')}:</span>
                  <Link 
                    to={`/buildings/${floor.buildingId}`} 
                    className="font-medium text-primary underline underline-offset-4 hover:text-primary/80 transition-colors cursor-pointer"
                  >
                    {buildingName}
                  </Link>
                </div>

                <div className="flex items-center gap-1.5 font-mono">
                  <span>{t('floorNumber')}:</span>
                  <span className="font-semibold text-foreground">#{floor.floorNumber}</span>
                </div>
              </CardDescription>
            </div>

            <div className="flex items-center gap-2">
              <Badge variant="outline">
                {floorTypeToLabel(floor.floorType, t)}
              </Badge>
              <Badge variant="secondary" className="flex items-center gap-1">
                <Home className="h-3 w-3" />
                {apartmentCount} {t('tableApartments')}
              </Badge>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => navigate(`/buildings/${floor.buildingId}/floors/${floor.id}/edit`)}
              >
                <Edit className="w-3.5 h-3.5 mr-1 rtl:ml-1 rtl:mr-0" />
                {t('edit')}
              </Button>
            </div>
          </div>
        </CardHeader>
      </Card>

      {/* Embedded Apartments Section */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base font-semibold flex items-center gap-2">
              <Home className="h-4 w-4 text-primary" />
              {t('apartmentsCount')}
            </CardTitle>
            <Button 
              size="sm" 
              onClick={() => navigate(`/buildings/${floor.buildingId}/floors/${floor.id}/apartments/new`)}
            >
              <Plus className="w-3.5 h-3.5 mr-1.5 rtl:ml-1.5 rtl:mr-0" />
              {t('addApartment')}
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <ApartmentsList floorId={floor.id} />
        </CardContent>
      </Card>
    </div>
  );
}
