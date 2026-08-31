import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { buildingSchema, BuildingFormValues } from '../schemas/buildings.schema';
import { BuildingDto } from '../types/buildings.types';
import { 
  BuildingType, 
  Governorate, 
  BUILDING_TYPE_OPTIONS, 
  GOVERNORATE_OPTIONS 
} from '../constants/buildingEnums';
import { getBuildingTranslation } from '../constants/translations';
import { toBuildingForm } from '../utils/buildingMappers';
import { buildingsApi } from '../api/buildings.api';
import { useTranslation } from '@/shared/i18n';
import { MapPicker } from '@/shared/components/ui/MapPicker';
import { 
  Form, 
  FormControl, 
  FormField, 
  FormItem, 
  FormLabel, 
  FormMessage 
} from '@/app/components/ui/form';
import { Input } from '@/app/components/ui/input';
import { Button } from '@/app/components/ui/button';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '@/app/components/ui/select';
import { MapPin } from 'lucide-react';

interface BuildingFormProps {
  initialData?: BuildingDto;
  onSubmit: (data: BuildingFormValues) => void;
  isLoading?: boolean;
}

export function BuildingForm({ initialData, onSubmit, isLoading }: BuildingFormProps) {
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);

  const form = useForm<BuildingFormValues>({
    resolver: zodResolver(buildingSchema),
    defaultValues: initialData ? toBuildingForm(initialData) : {
      name: '',
      internalCode: '',
      buildingType: BuildingType.Residential,
      totalFloors: 1,
      constructionYear: undefined,
      gpsLatitude: undefined,
      gpsLongitude: undefined,
      address: {
        governorate: Governorate.Amman,
        district: '',
        area: '',
        streetName: '',
        postalCode: '',
      },
    },
  });

  React.useEffect(() => {
    if (initialData) return;
    buildingsApi.getNextCode().then(({ value }) => {
      if (!form.getFieldState('internalCode').isDirty && !form.getValues('internalCode'))
        form.setValue('internalCode', value);
    }).catch(() => undefined);
  }, []);

  const watchLat = form.watch('gpsLatitude');
  const watchLng = form.watch('gpsLongitude');

  const handleLocationSelect = (lat: number, lng: number) => {
    form.setValue('gpsLatitude', lat, { shouldValidate: true, shouldDirty: true });
    form.setValue('gpsLongitude', lng, { shouldValidate: true, shouldDirty: true });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-4xl" noValidate>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          
          {/* General Information Column */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold border-b pb-2 text-foreground">{t('generalInfo')}</h3>
            
            {/* Building Name - REQUIRED */}
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('name')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('namePlaceholder')} 
                      aria-required="true"
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Internal Code - OPTIONAL; the backend does not generate one */}
            <FormField
              control={form.control}
              name="internalCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('internalCode')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('codePlaceholder')} 
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                    />
                  </FormControl>
                  {!initialData && <p className="text-xs text-muted-foreground">{language === 'ar' ? 'تم توليد الرقم تلقائيًا ويمكن تعديله.' : 'Generated automatically and can be edited.'}</p>}
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Building Type - REQUIRED */}
            <FormField
              control={form.control}
              name="buildingType"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('buildingType')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <Select 
                    onValueChange={(val) => field.onChange(Number(val))} 
                    value={field.value !== undefined ? String(field.value) : String(BuildingType.Residential)}
                  >
                    <FormControl>
                      <SelectTrigger aria-required="true">
                        <SelectValue placeholder={t('selectTypePlaceholder')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {BUILDING_TYPE_OPTIONS.map((opt) => (
                        <SelectItem key={opt.value} value={String(opt.value)}>
                          {t(opt.labelKey)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Total Floors - REQUIRED */}
            <FormField
              control={form.control}
              name="totalFloors"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('totalFloors')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      min={1}
                      max={200}
                      placeholder={t('floorsPlaceholder')}
                      aria-required="true"
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                      onChange={e => field.onChange(parseInt(e.target.value, 10) || 0)} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Construction Year - OPTIONAL */}
            <FormField
              control={form.control}
              name="constructionYear"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('constructionYear')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      placeholder={t('yearPlaceholder')} 
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                      value={field.value || ''}
                      onChange={e => field.onChange(e.target.value ? parseInt(e.target.value, 10) : undefined)} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          {/* Location & Address Column */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold border-b pb-2 text-foreground">{t('locationAndAddress')}</h3>

            {/* Governorate - REQUIRED */}
            <FormField
              control={form.control}
              name="address.governorate"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('governorate')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <Select 
                    onValueChange={(val) => field.onChange(Number(val))} 
                    value={field.value !== undefined ? String(field.value) : String(Governorate.Amman)}
                  >
                    <FormControl>
                      <SelectTrigger aria-required="true">
                        <SelectValue placeholder={t('selectGovernoratePlaceholder')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {GOVERNORATE_OPTIONS.map((opt) => (
                        <SelectItem key={opt.value} value={String(opt.value)}>
                          {t(opt.labelKey)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* City / District - REQUIRED by the backend command */}
            <FormField
              control={form.control}
              name="address.district"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('district')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('districtPlaceholder')} 
                      aria-required="true"
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Area / Neighborhood - REQUIRED by the backend command */}
            <FormField
              control={form.control}
              name="address.area"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('area')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('areaPlaceholder')} 
                      aria-required="true"
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                      value={field.value || ''} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Street Name - OPTIONAL */}
            <FormField
              control={form.control}
              name="address.streetName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('streetName')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('streetPlaceholder')} 
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                      value={field.value || ''} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Postal Code - OPTIONAL */}
            <FormField
              control={form.control}
              name="address.postalCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('postalCode')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('postalCodePlaceholder')} 
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal"
                      {...field} 
                      value={field.value || ''} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* GPS Coordinates Section with Map Picker */}
        <div className="space-y-4 border-t pt-6">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold flex items-center gap-2 text-foreground">
              <MapPin className="h-5 w-5 text-primary" />
              {t('gpsAndMapTitle')}
              <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
            </h3>
            {watchLat && watchLng && (
              <span className="text-xs text-muted-foreground font-mono bg-muted px-2 py-1 rounded">
                Lat: {watchLat.toFixed(6)}, Lng: {watchLng.toFixed(6)}
              </span>
            )}
          </div>

          <MapPicker
            lat={watchLat}
            lng={watchLng}
            onLocationSelect={handleLocationSelect}
            height="320px"
          />

          <div className="grid grid-cols-2 gap-4 pt-2">
            <FormField
              control={form.control}
              name="gpsLatitude"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 text-xs">
                    <span>{t('gpsLatitude')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      step="0.000001"
                      placeholder={t('gpsAutoFilledPlaceholder')}
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal text-xs font-mono"
                      {...field} 
                      value={field.value !== undefined ? field.value : ''}
                      onChange={e => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="gpsLongitude"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 text-xs">
                    <span>{t('gpsLongitude')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      step="0.000001"
                      placeholder={t('gpsAutoFilledPlaceholder')}
                      className="placeholder:text-muted-foreground/60 placeholder:font-normal text-xs font-mono"
                      {...field} 
                      value={field.value !== undefined ? field.value : ''}
                      onChange={e => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-end gap-4 mt-8 border-t pt-6">
          <Button 
            type="button" 
            variant="outline" 
            onClick={() => window.history.back()}
          >
            {t('cancel')}
          </Button>
          <Button 
            type="submit" 
            disabled={isLoading}
          >
            {isLoading ? t('saving') : t('save')}
          </Button>
        </div>
      </form>
    </Form>
  );
}
