import React from 'react';
import { BuildingDto } from '../types/buildings.types';
import { buildingTranslations as t } from '../constants/translations';
import { Badge } from '@/app/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Building, MapPin, Layers, Calendar, Code, Activity, AlertCircle } from 'lucide-react';

interface BuildingDetailsProps {
  building: BuildingDto;
}

export function BuildingDetails({ building }: BuildingDetailsProps) {
  return (
    <div className="space-y-6 max-w-6xl">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        
        {/* Main Details Card */}
        <Card className="md:col-span-2">
          <CardHeader>
            <div className="flex justify-between items-start">
              <div>
                <CardTitle className="text-2xl font-bold flex items-center gap-2">
                  <Building className="h-6 w-6 text-primary" />
                  {building.name}
                </CardTitle>
                <CardDescription className="mt-2 flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  {building.address 
                    ? `${building.address.streetName || ''}, ${building.address.district}, ${building.address.governorate}`
                    : 'No address provided'}
                </CardDescription>
              </div>
              <Badge variant={building.isActive ? "default" : "secondary"}>
                {building.isActive ? 'Active' : 'Inactive'}
              </Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-6 mt-4">
              <div className="space-y-1">
                <span className="text-sm text-muted-foreground flex items-center gap-1">
                  <Code className="h-4 w-4" /> {t.internalCode}
                </span>
                <p className="font-medium">{building.internalCode || '-'}</p>
              </div>
              
              <div className="space-y-1">
                <span className="text-sm text-muted-foreground flex items-center gap-1">
                  <Activity className="h-4 w-4" /> {t.buildingType}
                </span>
                <p className="font-medium">{building.buildingType}</p>
              </div>

              <div className="space-y-1">
                <span className="text-sm text-muted-foreground flex items-center gap-1">
                  <Layers className="h-4 w-4" /> {t.totalFloors}
                </span>
                <p className="font-medium">{building.totalFloors}</p>
              </div>

              <div className="space-y-1">
                <span className="text-sm text-muted-foreground flex items-center gap-1">
                  <Calendar className="h-4 w-4" /> {t.constructionYear}
                </span>
                <p className="font-medium">{building.constructionYear || '-'}</p>
              </div>
              
              <div className="space-y-1">
                <span className="text-sm text-muted-foreground flex items-center gap-1">
                  <Building className="h-4 w-4" /> {t.apartments}
                </span>
                <p className="font-medium">{building.totalApartmentsCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Location Map Placeholder */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Location Map</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col items-center justify-center h-48 bg-muted/30 rounded-md border border-dashed m-6 mt-0">
            {building.gpsLatitude && building.gpsLongitude ? (
              <div className="text-center">
                <MapPin className="h-8 w-8 text-primary mx-auto mb-2 opacity-50" />
                <p className="text-sm text-muted-foreground font-medium">Map View Available</p>
                <p className="text-xs text-muted-foreground mt-1">
                  {building.gpsLatitude.toFixed(4)}, {building.gpsLongitude.toFixed(4)}
                </p>
              </div>
            ) : (
              <div className="text-center">
                <AlertCircle className="h-8 w-8 text-muted-foreground mx-auto mb-2 opacity-30" />
                <p className="text-sm text-muted-foreground">No GPS coordinates</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Placeholders for Future Modules */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        
        <Card className="border-dashed shadow-none bg-muted/20">
          <CardHeader>
            <CardTitle className="text-lg text-muted-foreground flex items-center gap-2">
              <Building className="h-5 w-5" /> {t.apartments}
            </CardTitle>
          </CardHeader>
          <CardContent className="py-8 text-center text-muted-foreground">
            {t.notImplemented}
          </CardContent>
        </Card>
        
        <Card className="border-dashed shadow-none bg-muted/20">
          <CardHeader>
            <CardTitle className="text-lg text-muted-foreground flex items-center gap-2">
              <Activity className="h-5 w-5" /> {t.leaseStatistics}
            </CardTitle>
          </CardHeader>
          <CardContent className="py-8 text-center text-muted-foreground">
            {t.notImplemented}
          </CardContent>
        </Card>

        <Card className="border-dashed shadow-none bg-muted/20">
          <CardHeader>
            <CardTitle className="text-lg text-muted-foreground">
               {t.financialSummary}
            </CardTitle>
          </CardHeader>
          <CardContent className="py-8 text-center text-muted-foreground">
            {t.notImplemented}
          </CardContent>
        </Card>

        <Card className="border-dashed shadow-none bg-muted/20">
          <CardHeader>
            <CardTitle className="text-lg text-muted-foreground">
              {t.parking}
            </CardTitle>
          </CardHeader>
          <CardContent className="py-8 text-center text-muted-foreground">
            {t.notImplemented}
          </CardContent>
        </Card>

      </div>
    </div>
  );
}
