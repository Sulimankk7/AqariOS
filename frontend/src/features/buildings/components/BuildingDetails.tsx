import React from 'react';
import { useNavigate } from 'react-router';
import { BuildingDto } from '../types/buildings.types';
import { getBuildingTranslation } from '../constants/translations';
import { buildingTypeToLabel, governorateToLabel } from '../constants/buildingEnums';
import { useTranslation } from '@/shared/i18n';
import { FloorsList } from '@/features/floors/components/FloorsList';
import { useApartments } from '@/features/apartments/hooks/useApartments';
import { occupancyStatusToLabel, OccupancyStatus } from '@/features/apartments/constants/apartmentEnums';
import { getApartmentTranslation } from '@/features/apartments/constants/translations';
import { MapPicker } from '@/shared/components/ui/MapPicker';
import { Badge } from '@/app/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { 
  Building, 
  MapPin, 
  Layers, 
  Calendar, 
  Code, 
  Activity, 
  AlertCircle,
  Home,
  Car,
  FileText,
  Wallet,
  Plus,
  Eye
} from 'lucide-react';

interface BuildingDetailsProps {
  building: BuildingDto;
}

export function BuildingDetails({ building }: BuildingDetailsProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);
  const aptT = (key: string) => getApartmentTranslation(key, language);

  const { data: apartments, isLoading: isLoadingApartments } = useApartments({ buildingId: building.id });

  const getOccupancyBadgeVariant = (status: OccupancyStatus) => {
    switch (status) {
      case OccupancyStatus.Occupied: return 'default';
      case OccupancyStatus.Vacant: return 'secondary';
      case OccupancyStatus.UnderMaintenance: return 'destructive';
      case OccupancyStatus.Listed: return 'outline';
      default: return 'secondary';
    }
  };

  const totalCount = apartments ? apartments.length : building.totalApartmentsCount;

  return (
    <div className="space-y-6 max-w-6xl">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        
        {/* Main Details Card */}
        <Card className="md:col-span-2">
          <CardHeader>
            <div className="flex justify-between items-start">
              <div>
                <CardTitle className="text-2xl font-bold flex items-center gap-2 text-foreground">
                  <Building className="h-6 w-6 text-primary shrink-0" />
                  {building.name}
                </CardTitle>
                <CardDescription className="mt-2 flex items-center gap-2 text-muted-foreground">
                  <MapPin className="h-4 w-4 shrink-0" />
                  {building.address 
                    ? `${building.address.streetName ? building.address.streetName + ', ' : ''}${building.address.district ? building.address.district + ', ' : ''}${governorateToLabel(building.address.governorate, t)}`
                    : t('noAddress')}
                </CardDescription>
              </div>
              <Badge variant={building.isActive ? "default" : "secondary"}>
                {building.isActive ? t('active') : t('inactive')}
              </Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-6 mt-2 border-t pt-4">
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground flex items-center gap-1">
                  <Code className="h-3.5 w-3.5" /> {t('internalCode')}
                </span>
                <p className="font-medium text-sm">{building.internalCode || '-'}</p>
              </div>
              
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground flex items-center gap-1">
                  <Activity className="h-3.5 w-3.5" /> {t('buildingType')}
                </span>
                <p className="font-medium text-sm">{buildingTypeToLabel(building.buildingType, t)}</p>
              </div>

              <div className="space-y-1">
                <span className="text-xs text-muted-foreground flex items-center gap-1">
                  <Layers className="h-3.5 w-3.5" /> {t('totalFloors')}
                </span>
                <p className="font-medium text-sm">{building.totalFloors}</p>
              </div>

              <div className="space-y-1">
                <span className="text-xs text-muted-foreground flex items-center gap-1">
                  <Calendar className="h-3.5 w-3.5" /> {t('constructionYear')}
                </span>
                <p className="font-medium text-sm">{building.constructionYear || '-'}</p>
              </div>
              
              <div className="space-y-1">
                <span className="text-xs text-muted-foreground flex items-center gap-1">
                  <Home className="h-3.5 w-3.5" /> {t('apartments')}
                </span>
                <p className="font-medium text-sm">{totalCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Location Map Card */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <MapPin className="h-4 w-4 text-primary" />
              {t('locationMap')}
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0">
            {building.gpsLatitude && building.gpsLongitude ? (
              <div className="space-y-2">
                <MapPicker 
                  lat={building.gpsLatitude} 
                  lng={building.gpsLongitude} 
                  readOnly={true} 
                  height="180px" 
                />
                <p className="text-xs text-muted-foreground font-mono text-center">
                  GPS: {building.gpsLatitude.toFixed(6)}, {building.gpsLongitude.toFixed(6)}
                </p>
              </div>
            ) : (
              <div className="flex flex-col items-center justify-center h-44 bg-muted/20 rounded-md border border-dashed text-center p-4">
                <AlertCircle className="h-8 w-8 text-muted-foreground/40 mb-2" />
                <p className="text-sm font-medium text-muted-foreground">{t('noGpsRecorded')}</p>
                <p className="text-xs text-muted-foreground/70 mt-1">{t('editGpsInstruction')}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Floors Section - Child Resource of Building */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base font-semibold flex items-center gap-2">
              <Layers className="h-4 w-4 text-primary" />
              {t('totalFloors')}
            </CardTitle>
            <Button 
              size="sm" 
              onClick={() => navigate(`/buildings/${building.id}/floors/new`)}
            >
              <Plus className="w-3.5 h-3.5 mr-1.5 rtl:ml-1.5 rtl:mr-0" />
              Add Floor
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <FloorsList buildingId={building.id} />
        </CardContent>
      </Card>

      {/* Domain Modules Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        
        {/* Apartments Card - Real Data Display */}
        <Card className="shadow-xs md:col-span-2">
          <CardHeader className="pb-3">
            <CardTitle className="text-base font-semibold flex items-center justify-between">
              <span className="flex items-center gap-2">
                <Home className="h-4 w-4 text-primary" />
                {t('apartments')}
              </span>
              <Badge variant="outline" className="text-xs">
                {totalCount} {t('totalCount')}
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent className="pt-2">
            {isLoadingApartments ? (
              <div className="py-8 text-center text-xs text-muted-foreground">{aptT('loadingData')}</div>
            ) : apartments && apartments.length > 0 ? (
              <div className="overflow-x-auto border rounded-lg">
                <table className="w-full text-sm text-left rtl:text-right">
                  <thead className="bg-muted/50 text-xs font-semibold text-muted-foreground border-b uppercase">
                    <tr>
                      <th className="p-3">{aptT('tableUnit')}</th>
                      <th className="p-3">{aptT('tableArea')}</th>
                      <th className="p-3">{aptT('bedrooms')} / {aptT('bathrooms')}</th>
                      <th className="p-3">{aptT('tableOccupancy')}</th>
                      <th className="p-3">{aptT('tableBaseRent')}</th>
                      <th className="p-3 text-end"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {apartments.map((apt) => (
                      <tr key={apt.id} className="hover:bg-muted/20 transition-colors">
                        <td className="p-3 font-medium text-foreground">
                          {apt.unitNumber}
                        </td>
                        <td className="p-3 text-muted-foreground">{apt.areaSqm} m²</td>
                        <td className="p-3 text-muted-foreground">{apt.bedrooms} BDR | {apt.bathrooms} BTH</td>
                        <td className="p-3">
                          <Badge variant={getOccupancyBadgeVariant(apt.occupancyStatus)}>
                            {occupancyStatusToLabel(apt.occupancyStatus, aptT)}
                          </Badge>
                        </td>
                        <td className="p-3 font-medium">
                          {apt.baseRentAmount ? `${apt.baseRentAmount} ${apt.baseRentCurrency}` : '-'}
                        </td>
                        <td className="p-3 text-end">
                          <Button 
                            variant="ghost" 
                            size="icon"
                            title={`View Unit ${apt.unitNumber}`}
                            onClick={() => navigate(`/apartments/${apt.id}`)}
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="py-8 text-center">
                <Home className="h-8 w-8 text-muted-foreground/40 mx-auto mb-2" />
                <p className="text-sm text-muted-foreground font-medium">{t('noApartments')}</p>
              </div>
            )}
          </CardContent>
        </Card>

      </div>
    </div>
  );
}
