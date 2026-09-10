import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { 
  apartmentSchema, 
  ApartmentFormInput,
  ApartmentFormValues, 
  SUPPORTED_CURRENCIES 
} from '../schemas/apartments.schema';
import { ApartmentDto } from '../types/apartments.types';
import { OwnershipStatus, OWNERSHIP_STATUS_OPTIONS } from '../constants/apartmentEnums';
import { getApartmentTranslation } from '../constants/translations';
import { getFloorTranslation } from '@/features/floors/constants/translations';
import { toApartmentForm } from '../utils/apartmentMappers';
import { useFloors, useFloor } from '@/features/floors/hooks/useFloors';
import { useBuildings } from '@/features/buildings/hooks/useBuildings';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';
import { apartmentsApi } from '../api/apartments.api';
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
import { AlertCircle, Plus } from 'lucide-react';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '@/app/components/ui/select';

export interface FloorOption {
  id: string;
  floorNumber: number;
  label?: string;
}

interface ApartmentFormProps {
  initialData?: ApartmentDto;
  defaultFloorId?: string;
  buildingId?: string;
  buildingName?: string;
  isEditMode?: boolean;
  onSubmit: (data: ApartmentFormValues) => void;
  isLoading?: boolean;
}

export function ApartmentForm({ 
  initialData, 
  defaultFloorId, 
  buildingId: initialBuildingId,
  buildingName,
  isEditMode = false,
  onSubmit, 
  isLoading 
}: ApartmentFormProps) {
  const navigate = useNavigate();
  const { language, t: sharedT } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);
  const floorT = (key: string) => getFloorTranslation(key, language);

  const { data: defaultFloor } = useFloor(defaultFloorId || initialData?.floorId);
  const { data: buildingsData, error: buildingsError, refetch: refetchBuildings } = useBuildings();

  const [selectedBuildingId, setSelectedBuildingId] = useState<string>(
    initialData?.buildingId || initialBuildingId || defaultFloor?.buildingId || ''
  );

  useEffect(() => {
    if (!selectedBuildingId && defaultFloor?.buildingId) {
      setSelectedBuildingId(defaultFloor.buildingId);
    }
  }, [defaultFloor, selectedBuildingId]);

  const { data: floorsData, isLoading: isLoadingFloors, error: floorsError, refetch: refetchFloors } = useFloors(selectedBuildingId);
  const [unitSuggestionUnavailable, setUnitSuggestionUnavailable] = useState(false);

  const form = useForm<ApartmentFormInput, unknown, ApartmentFormValues>({
    resolver: zodResolver(apartmentSchema),
    defaultValues: initialData 
      ? toApartmentForm(initialData) 
      : {
          floorId: defaultFloorId || '',
          unitNumber: '',
          areaSqm: 0,
          ownershipStatus: OwnershipStatus.CompanyOwned,
          externalOwnerName: '',
          externalOwnerPhone: '',
          bedrooms: 0,
          bathrooms: 0,
          baseRentAmount: undefined,
          baseRentCurrency: 'JOD',
        },
  });

  const watchOwnership = form.watch('ownershipStatus');
  const selectedFloorId = form.watch('floorId');

  useEffect(() => {
    if (initialData || !selectedFloorId) return;
    setUnitSuggestionUnavailable(false);
    apartmentsApi.getNextUnitNumber(selectedFloorId).then(({ value }) => {
      if (!form.getFieldState('unitNumber').isDirty) form.setValue('unitNumber', value);
    }).catch((error) => {
      if (import.meta.env.DEV) console.warn('[apartments] Next unit number suggestion unavailable', error);
      setUnitSuggestionUnavailable(true);
    });
  }, [selectedFloorId, initialData, form]);

  // Requirement 3: Automatically clear external owner fields when Ownership Model is not ThirdPartyOwned
  useEffect(() => {
    if (watchOwnership !== OwnershipStatus.ThirdPartyOwned) {
      form.setValue('externalOwnerName', undefined);
      form.setValue('externalOwnerPhone', undefined);
    }
  }, [watchOwnership, form]);

  const floorOptions: FloorOption[] = floorsData && floorsData.length > 0 
    ? floorsData.map(f => ({
        id: f.id,
        floorNumber: f.floorNumber,
        label: f.floorLabel || `Floor ${f.floorNumber}`
      }))
    : (defaultFloor ? [{
        id: defaultFloor.id,
        floorNumber: defaultFloor.floorNumber,
        label: defaultFloor.floorLabel || `Floor ${defaultFloor.floorNumber}`
      }] : []);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-4xl" noValidate>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          
          {/* Unit & Physical Details Column */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold border-b pb-2 text-foreground">{t('unitInfo')}</h3>

            {initialBuildingId && !initialData && (
              <FormItem>
                <FormLabel className="font-medium">{t('buildingId')}</FormLabel>
                <FormControl>
                  <Input value={buildingName || ''} readOnly aria-readonly="true" className="bg-muted/40" />
                </FormControl>
              </FormItem>
            )}

            {/* Building Select if building is not preset */}
            {!initialData && !initialBuildingId && !defaultFloorId && (
              <FormItem>
                <FormLabel className="flex items-center gap-1 font-medium">
                  <span>{t('buildingId')}</span>
                  <span className="text-destructive font-bold" aria-hidden="true">*</span>
                </FormLabel>
                <Select 
                  onValueChange={(val) => {
                    setSelectedBuildingId(val);
                    form.setValue('floorId', '');
                  }}
                  value={selectedBuildingId}
                >
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder={t('selectBuildingPlaceholder')} />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {buildingsData?.map((b) => (
                      <SelectItem key={b.id} value={b.id}>
                        {b.internalCode ? `${b.name} (${b.internalCode})` : b.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {buildingsError && <div role="alert" className="mt-2 flex items-center justify-between gap-3 text-xs text-destructive"><span>{extractUserFriendlyError(buildingsError, sharedT('errors.generic'))}</span><Button type="button" size="sm" variant="outline" onClick={() => refetchBuildings()}>{sharedT('common.retry')}</Button></div>}
              </FormItem>
            )}

            {/* Floor ID - MUST ALWAYS BE A CONSTRAINED SELECT */}
            <FormField
              control={form.control}
              name="floorId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('floorId')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <Select 
                    onValueChange={field.onChange} 
                    value={field.value || ''}
                    disabled={isEditMode}
                  >
                    <FormControl>
                      <SelectTrigger aria-required="true" className="w-full">
                        <SelectValue placeholder={isLoadingFloors ? sharedT('common.loading') : t('floorIdPlaceholder')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {floorOptions.map((floor) => (
                        <SelectItem key={floor.id} value={floor.id}>
                          {floor.label || `Floor ${floor.floorNumber}`}
                        </SelectItem>
                      ))}
                      {floorOptions.length === 0 && field.value && (
                        <SelectItem value={field.value}>
                          {t('floor')}
                        </SelectItem>
                      )}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {selectedBuildingId && floorsError && (
              <div role="alert" className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
                <div className="flex items-center justify-between gap-3"><span>{extractUserFriendlyError(floorsError, sharedT('errors.generic'))}</span><Button type="button" size="sm" variant="outline" onClick={() => refetchFloors()}>{sharedT('common.retry')}</Button></div>
              </div>
            )}

            {initialBuildingId && !isLoadingFloors && !floorsError && floorOptions.length === 0 && (
              <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-700 dark:text-amber-300">
                <div className="flex items-start gap-3">
                  <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
                  <div className="space-y-3">
                    <p>{floorT('noFloors')}</p>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() => navigate(`/buildings/${initialBuildingId}/floors/new`)}
                    >
                      <Plus className="h-4 w-4 mr-2 rtl:ml-2 rtl:mr-0" />
                      {floorT('addFloor')}
                    </Button>
                  </div>
                </div>
              </div>
            )}
            
            {/* Unit Number - REQUIRED */}
            <FormField
              control={form.control}
              name="unitNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('unitNumber')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      placeholder={t('unitNumberPlaceholder')} 
                      aria-required="true"
                      disabled={isEditMode}
                      className="placeholder:text-muted-foreground/60"
                      {...field} 
                    />
                  </FormControl>
                  {!isEditMode && <p className="text-xs text-muted-foreground">{language === 'ar' ? 'تم توليد الرقم تلقائيًا ويمكن تعديله.' : 'Generated automatically and can be edited.'}</p>}
                  {unitSuggestionUnavailable && <p className="text-xs text-muted-foreground">{sharedT('errors.generic')}</p>}
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Area Sqm - REQUIRED */}
            <FormField
              control={form.control}
              name="areaSqm"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('areaSqm')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      step="0.1"
                      min={0.01}
                      placeholder={t('areaSqmPlaceholder')} 
                      aria-required="true"
                      disabled={isEditMode}
                      className="placeholder:text-muted-foreground/60"
                      {...field} 
                      onChange={e => field.onChange(e.target.value ? parseFloat(e.target.value) : 0)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Bedrooms - OPTIONAL */}
            <FormField
              control={form.control}
              name="bedrooms"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('bedrooms')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      min={0}
                      placeholder={t('bedroomsPlaceholder')}
                      disabled={isEditMode}
                      {...field} 
                      onChange={e => field.onChange(parseInt(e.target.value, 10) || 0)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Bathrooms - OPTIONAL */}
            <FormField
              control={form.control}
              name="bathrooms"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('bathrooms')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      min={0}
                      placeholder={t('bathroomsPlaceholder')}
                      disabled={isEditMode}
                      {...field} 
                      onChange={e => field.onChange(parseInt(e.target.value, 10) || 0)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          {/* Ownership & Financial Terms Column */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold border-b pb-2 text-foreground">{t('financialTerms')}</h3>

            {/* Base Rent Amount - OPTIONAL */}
            <FormField
              control={form.control}
              name="baseRentAmount"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('baseRentAmount')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      step="0.01"
                      placeholder={t('baseRentAmountPlaceholder')}
                      {...field} 
                      value={field.value !== undefined ? field.value : ''}
                      onChange={e => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Base Rent Currency - Constrained Select */}
            <FormField
              control={form.control}
              name="baseRentCurrency"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('baseRentCurrency')}</span>
                    <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                  </FormLabel>
                  <Select onValueChange={field.onChange} value={field.value || 'JOD'}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="JOD" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {SUPPORTED_CURRENCIES.map((curr) => (
                        <SelectItem key={curr} value={curr}>
                          {curr}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <h3 className="text-lg font-semibold border-b pb-2 pt-2 text-foreground">{t('ownershipInfo')}</h3>

            {/* Ownership Status - REQUIRED */}
            <FormField
              control={form.control}
              name="ownershipStatus"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="flex items-center gap-1 font-medium">
                    <span>{t('ownershipStatusTitle')}</span>
                    <span className="text-destructive font-bold" aria-hidden="true">*</span>
                    <span className="sr-only">{t('required')}</span>
                  </FormLabel>
                  <Select 
                    onValueChange={(val) => field.onChange(Number(val))} 
                    value={String(field.value)}
                    disabled={isEditMode}
                  >
                    <FormControl>
                      <SelectTrigger aria-required="true">
                        <SelectValue placeholder={t('selectOwnershipPlaceholder')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {OWNERSHIP_STATUS_OPTIONS.map((opt) => (
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

            {/* Requirement 3: External Owner Fields MUST only be shown/enabled when Ownership Model === ThirdPartyOwned */}
            {watchOwnership === OwnershipStatus.ThirdPartyOwned && (
              <>
                <FormField
                  control={form.control}
                  name="externalOwnerName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1 font-medium">
                        <span>{t('externalOwnerName')}</span>
                        <span className="text-destructive font-bold" aria-hidden="true">*</span>
                        <span className="sr-only">{t('required')}</span>
                      </FormLabel>
                      <FormControl>
                        <Input 
                          placeholder={t('externalOwnerNamePlaceholder')} 
                          aria-required="true"
                          disabled={isEditMode}
                          {...field} 
                          value={field.value || ''}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="externalOwnerPhone"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="flex items-center gap-1 font-medium">
                        <span>{t('externalOwnerPhone')}</span>
                        <span className="text-muted-foreground font-normal text-xs ms-1.5">{t('optional')}</span>
                      </FormLabel>
                      <FormControl>
                        <Input 
                          placeholder={t('externalOwnerPhonePlaceholder')} 
                          disabled={isEditMode}
                          {...field} 
                          value={field.value || ''}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </>
            )}
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
            disabled={isLoading || (!!initialBuildingId && !isLoadingFloors && !floorsError && floorOptions.length === 0)}
          >
            {isLoading ? t('saving') : t('save')}
          </Button>
        </div>
      </form>
    </Form>
  );
}
