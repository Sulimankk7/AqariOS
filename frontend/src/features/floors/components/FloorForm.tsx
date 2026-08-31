import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { floorSchema, FloorFormValues } from '../schemas/floors.schema';
import { FloorDto } from '../types/floors.types';
import { FloorType, FLOOR_TYPE_OPTIONS } from '../constants/floorEnums';
import { getFloorTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { floorsApi } from '../api/floors.api';
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

interface FloorFormProps {
  buildingId?: string;
  initialData?: FloorDto;
  isEditMode?: boolean;
  onSubmit: (data: FloorFormValues) => void;
  isLoading?: boolean;
}

export function FloorForm({ 
  buildingId,
  initialData, 
  isEditMode = false, 
  onSubmit, 
  isLoading 
}: FloorFormProps) {
  const { language } = useTranslation();
  const t = (key: string) => getFloorTranslation(key, language);

  const form = useForm<FloorFormValues>({
    resolver: zodResolver(floorSchema),
    defaultValues: initialData 
      ? {
          floorNumber: initialData.floorNumber,
          floorLabel: initialData.floorLabel || '',
          floorType: initialData.floorType ?? FloorType.Standard,
        }
      : {
          floorNumber: 1,
          floorLabel: '',
          floorType: FloorType.Standard,
        },
  });

  React.useEffect(() => {
    if (initialData || !buildingId) return;
    floorsApi.getNextFloorNumber(buildingId).then(({ value }) => {
      if (!form.getFieldState('floorNumber').isDirty) form.setValue('floorNumber', Number(value));
    }).catch(() => undefined);
  }, [buildingId]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-2xl" noValidate>
        
        {/* Floor Number - REQUIRED */}
        <FormField
          control={form.control}
          name="floorNumber"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="flex items-center gap-1 font-medium">
                <span>{t('floorNumber')}</span>
                <span className="text-destructive font-bold" aria-hidden="true">*</span>
              </FormLabel>
              <FormControl>
                <Input 
                  type="number" 
                  min={-5}
                  max={200}
                  placeholder={t('numberPlaceholder')} 
                  aria-required="true"
                  disabled={isEditMode}
                  className="placeholder:text-muted-foreground/60"
                  {...field} 
                  onChange={e => field.onChange(parseInt(e.target.value, 10) || 0)}
                />
              </FormControl>
              {!isEditMode && <p className="text-xs text-muted-foreground">{language === 'ar' ? 'تم توليد الرقم تلقائيًا ويمكن تعديله.' : 'Generated automatically and can be edited.'}</p>}
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Floor Label - REQUIRED */}
        <FormField
          control={form.control}
          name="floorLabel"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="flex items-center gap-1 font-medium">
                <span>{t('floorLabel')}</span>
                <span className="text-destructive font-bold" aria-hidden="true">*</span>
              </FormLabel>
              <FormControl>
                <Input 
                  placeholder={t('labelPlaceholder')} 
                  aria-required="true"
                  className="placeholder:text-muted-foreground/60"
                  {...field} 
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Floor Type - REQUIRED */}
        <FormField
          control={form.control}
          name="floorType"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="flex items-center gap-1 font-medium">
                <span>{t('floorType')}</span>
                <span className="text-destructive font-bold" aria-hidden="true">*</span>
              </FormLabel>
              <Select 
                onValueChange={(val) => field.onChange(Number(val))} 
                value={String(field.value)}
              >
                <FormControl>
                  <SelectTrigger aria-required="true">
                    <SelectValue placeholder={t('selectTypePlaceholder')} />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {FLOOR_TYPE_OPTIONS.map((opt) => (
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
