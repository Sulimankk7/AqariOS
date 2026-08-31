import React from 'react';
import { Link } from 'react-router';
import { ApartmentDto } from '../types/apartments.types';
import { getApartmentTranslation } from '../constants/translations';
import { ownershipStatusToLabel, occupancyStatusToLabel, OccupancyStatus } from '../constants/apartmentEnums';
import { useBuilding } from '@/features/buildings/hooks/useBuildings';
import { useFloor } from '@/features/floors/hooks/useFloors';
import { useTranslation } from '@/shared/i18n';
import { Badge } from '@/app/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { 
  Home, 
  Layers, 
  Building2,
  UserCheck, 
  Wallet, 
  Bed, 
  Bath, 
  Maximize, 
  Phone,
  User
} from 'lucide-react';

interface ApartmentDetailsProps {
  apartment: ApartmentDto;
  floorLabel?: string;
}

export function ApartmentDetails({ apartment, floorLabel }: ApartmentDetailsProps) {
  const { language } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);

  const { data: building } = useBuilding(apartment.buildingId);
  const { data: floor } = useFloor(apartment.floorId);

  const getOccupancyBadgeVariant = (status: OccupancyStatus) => {
    switch (status) {
      case OccupancyStatus.Occupied: return 'default';
      case OccupancyStatus.Vacant: return 'secondary';
      case OccupancyStatus.UnderMaintenance: return 'destructive';
      case OccupancyStatus.Listed: return 'outline';
      default: return 'secondary';
    }
  };

  const resolvedFloorLabel = floor 
    ? (floor.floorLabel || `Floor ${floor.floorNumber}`)
    : (floorLabel || t('floor'));

  const buildingName = building?.name || t('building');

  return (
    <div className="space-y-6 max-w-5xl">
      <Card>
        <CardHeader>
          <div className="flex justify-between items-start">
            <div>
              <CardTitle className="text-2xl font-bold flex items-center gap-2 text-foreground">
                <Home className="h-6 w-6 text-primary shrink-0" />
                {t('unitNumber')}: {apartment.unitNumber}
              </CardTitle>
              <CardDescription className="mt-2 flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
                {/* Clickable Building Name Link with clear link styling */}
                <div className="flex items-center gap-1.5">
                  <Building2 className="h-3.5 w-3.5 text-primary" />
                  <span>{t('building')}:</span>
                  <Link 
                    to={`/buildings/${apartment.buildingId}`} 
                    className="font-medium text-primary underline underline-offset-4 hover:text-primary/80 transition-colors cursor-pointer"
                  >
                    {buildingName}
                  </Link>
                </div>

                {/* Resolved Floor Label */}
                <div className="flex items-center gap-1.5">
                  <Layers className="h-3.5 w-3.5 text-primary" />
                  <span>{t('floor')}:</span>
                  <span className="font-medium text-foreground">{resolvedFloorLabel}</span>
                </div>
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Badge variant={getOccupancyBadgeVariant(apartment.occupancyStatus)}>
                {occupancyStatusToLabel(apartment.occupancyStatus, t)}
              </Badge>
              <Badge variant="outline">
                {ownershipStatusToLabel(apartment.ownershipStatus, t)}
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-6 border-t pt-4">
          
          {/* Key Attributes Grid */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-6">
            <div className="space-y-1">
              <span className="text-xs text-muted-foreground flex items-center gap-1">
                <Maximize className="h-3.5 w-3.5" /> {t('areaSqm')}
              </span>
              <p className="font-semibold text-sm">{apartment.areaSqm} m²</p>
            </div>

            <div className="space-y-1">
              <span className="text-xs text-muted-foreground flex items-center gap-1">
                <Bed className="h-3.5 w-3.5" /> {t('bedrooms')}
              </span>
              <p className="font-semibold text-sm">{apartment.bedrooms}</p>
            </div>

            <div className="space-y-1">
              <span className="text-xs text-muted-foreground flex items-center gap-1">
                <Bath className="h-3.5 w-3.5" /> {t('bathrooms')}
              </span>
              <p className="font-semibold text-sm">{apartment.bathrooms}</p>
            </div>

            <div className="space-y-1">
              <span className="text-xs text-muted-foreground flex items-center gap-1">
                <Wallet className="h-3.5 w-3.5" /> {t('baseRentAmount')}
              </span>
              <p className="font-semibold text-sm">
                {apartment.baseRentAmount ? `${apartment.baseRentAmount} ${apartment.baseRentCurrency}` : '-'}
              </p>
            </div>
          </div>

          {/* External Owner Information if third-party owned */}
          {apartment.externalOwnerName && (
            <div className="border-t pt-4 space-y-2">
              <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-1.5">
                <UserCheck className="h-3.5 w-3.5 text-primary" />
                {t('ownershipInfo')}
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 bg-muted/30 p-3 rounded-lg border">
                <div className="flex items-center gap-2 text-sm">
                  <User className="h-4 w-4 text-muted-foreground" />
                  <span className="text-muted-foreground">{t('externalOwnerName')}:</span>
                  <span className="font-medium">{apartment.externalOwnerName}</span>
                </div>
                {apartment.externalOwnerPhone && (
                  <div className="flex items-center gap-2 text-sm">
                    <Phone className="h-4 w-4 text-muted-foreground" />
                    <span className="text-muted-foreground">{t('externalOwnerPhone')}:</span>
                    <span className="font-medium font-mono">{apartment.externalOwnerPhone}</span>
                  </div>
                )}
              </div>
            </div>
          )}

        </CardContent>
      </Card>
    </div>
  );
}
