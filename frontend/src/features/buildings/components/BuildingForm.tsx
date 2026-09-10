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
import { locationsApi } from '../api/locations.api';
import type {
  AddressEnrichment,
  AddressField,
} from '../utils/reverseGeocoding';
import {
  applySuggestedAddress,
  hasApplicableSuggestion,
  isCurrentGeocodingResponse,
  meaningfulFormattedAddress,
  mergeAutomaticEnrichment,
  toAddressEnrichment,
} from '../utils/reverseGeocoding';
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

interface BuildingFormProps {
  initialData?: BuildingDto;
  onSubmit: (data: BuildingFormValues) => void;
  isLoading?: boolean;
}

export function BuildingForm({ initialData, onSubmit, isLoading }: BuildingFormProps) {
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);
  const lookupAbort = React.useRef<AbortController | null>(null);
  const lookupSequence = React.useRef(0);
  const manuallyEditedAddressFields = React.useRef(new Set<AddressField>(initialData
    ? (['governorate', 'district', 'area', 'streetName', 'postalCode'] as AddressField[]).filter((field) => {
        const value = toBuildingForm(initialData).address[field];
        return field === 'governorate' || (typeof value === 'string' && value.trim().length > 0);
      })
    : []));
  const [isGeocoding, setIsGeocoding] = React.useState(false);
  const [geocodingFailed, setGeocodingFailed] = React.useState(false);
  const [addressSuggestion, setAddressSuggestion] = React.useState<{
    formattedAddress: string;
    enrichment: AddressEnrichment;
  } | null>(null);

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

  React.useEffect(() => () => lookupAbort.current?.abort(), []);

  const watchLat = form.watch('gpsLatitude');
  const watchLng = form.watch('gpsLongitude');
  const watchAddress = form.watch('address');

  const markAddressFieldManual = (field: AddressField) => {
    manuallyEditedAddressFields.current.add(field);
  };

  const applyAddressPatch = (patch: Partial<BuildingFormValues['address']>) => {
    if (patch.governorate !== undefined) form.setValue('address.governorate', patch.governorate, { shouldValidate: true });
    if (patch.district !== undefined) form.setValue('address.district', patch.district, { shouldValidate: true });
    if (patch.area !== undefined) form.setValue('address.area', patch.area, { shouldValidate: true });
    if (patch.streetName !== undefined) form.setValue('address.streetName', patch.streetName, { shouldValidate: true });
    if (patch.postalCode !== undefined) form.setValue('address.postalCode', patch.postalCode, { shouldValidate: true });
  };

  const handleLocationSelect = async (lat: number, lng: number) => {
    form.setValue('gpsLatitude', lat, { shouldValidate: true, shouldDirty: true });
    form.setValue('gpsLongitude', lng, { shouldValidate: true, shouldDirty: true });
    lookupAbort.current?.abort();
    const controller = new AbortController();
    lookupAbort.current = controller;
    const sequence = ++lookupSequence.current;
    setIsGeocoding(true);
    setGeocodingFailed(false);
    setAddressSuggestion(null);
    try {
      const result = await locationsApi.reverseGeocode(lat, lng, language, controller.signal);
      if (!isCurrentGeocodingResponse(sequence, lookupSequence.current, controller.signal.aborted)) return;
      const current = form.getValues('address');
      const enrichment = toAddressEnrichment(result);
      const formattedAddress = meaningfulFormattedAddress(result);

      applyAddressPatch(mergeAutomaticEnrichment(current, enrichment, manuallyEditedAddressFields.current));
      setAddressSuggestion(formattedAddress ? { formattedAddress, enrichment } : null);
    } catch (error) {
      if (!controller.signal.aborted && sequence === lookupSequence.current) setGeocodingFailed(true);
    } finally {
      if (sequence === lookupSequence.current) setIsGeocoding(false);
    }
  };

  const canApplySuggestion = addressSuggestion !== null && hasApplicableSuggestion(
    watchAddress,
    addressSuggestion.enrichment,
    manuallyEditedAddressFields.current,
  );

  const handleUseSuggestedAddress = () => {
    if (!addressSuggestion) return;
    applyAddressPatch(applySuggestedAddress(
      form.getValues('address'),
      addressSuggestion.enrichment,
      manuallyEditedAddressFields.current,
    ));
  };

  const mapEnrichmentSection = (
    <div className="space-y-3">
      <MapPicker
        lat={watchLat}
        lng={watchLng}
        onLocationSelect={handleLocationSelect}
        height="280px"
        instruction={language === 'ar' ? 'انقر على الخريطة لتحديد موقع المبنى' : 'Click on the map to select the building location'}
      />
      <p
        className="rounded-md border bg-muted/30 px-3 py-2 text-sm text-muted-foreground"
        dir="rtl"
        role="note"
      >
        قد تكون بعض تفاصيل العنوان غير دقيقة. يرجى التحقق من العنوان والموقع على الخريطة.
      </p>
      {isGeocoding && <p role="status" className="text-sm text-muted-foreground">{t('geocodingLoading')}</p>}
      {geocodingFailed && <p role="status" className="text-sm text-amber-700">{t('geocodingFailure')}</p>}
      {addressSuggestion && (
        <div className="rounded-md border bg-muted/30 p-3" role="status">
          <p className="text-xs font-medium text-muted-foreground">{t('suggestedAddress')}</p>
          <p className="mt-1 text-sm text-foreground" dir="auto">{addressSuggestion.formattedAddress}</p>
          {canApplySuggestion ? (
            <Button type="button" variant="outline" size="sm" className="mt-3" onClick={handleUseSuggestedAddress}>
              {t('useSuggestedAddress')}
            </Button>
          ) : (!addressSuggestion.enrichment.district
            || !addressSuggestion.enrichment.area
            || !addressSuggestion.enrichment.streetName) ? (
            <p className="mt-2 text-xs text-muted-foreground">{t('addressDetailsManual')}</p>
          ) : null}
        </div>
      )}
    </div>
  );

  const coordinateFields = (
    <details className="rounded-md border bg-muted/20 p-3">
      <summary className="cursor-pointer text-sm font-medium text-muted-foreground">{t('coordinatesDetails')}</summary>
      <div className="grid grid-cols-2 gap-4 pt-3">
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
    </details>
  );

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
                  <p className="text-xs text-muted-foreground">{t('licensedFloorsDescription')}</p>
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

            {mapEnrichmentSection}

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
                    onValueChange={(val) => {
                      markAddressFieldManual('governorate');
                      field.onChange(Number(val));
                    }}
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
                      onChange={(event) => {
                        markAddressFieldManual('district');
                        field.onChange(event);
                      }}
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
                      onChange={(event) => {
                        markAddressFieldManual('area');
                        field.onChange(event);
                      }}
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
                      onChange={(event) => {
                        markAddressFieldManual('streetName');
                        field.onChange(event);
                      }}
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
                      onChange={(event) => {
                        markAddressFieldManual('postalCode');
                        field.onChange(event);
                      }}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {coordinateFields}
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
