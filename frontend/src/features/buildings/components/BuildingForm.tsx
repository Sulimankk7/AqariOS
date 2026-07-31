import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { buildingSchema, BuildingFormValues } from '../schemas/buildings.schema';
import { BuildingType, Governorate, BuildingDto } from '../types/buildings.types';
import { buildingTranslations as t } from '../constants/translations';
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
  const form = useForm<BuildingFormValues>({
    resolver: zodResolver(buildingSchema),
    defaultValues: {
      name: initialData?.name || '',
      internalCode: initialData?.internalCode || '',
      buildingType: initialData?.buildingType || BuildingType.Residential,
      totalFloors: initialData?.totalFloors || 1,
      constructionYear: initialData?.constructionYear || undefined,
      gpsLatitude: initialData?.gpsLatitude || undefined,
      gpsLongitude: initialData?.gpsLongitude || undefined,
      address: {
        governorate: initialData?.address?.governorate || Governorate.Amman,
        district: initialData?.address?.district || '',
        area: initialData?.address?.area || '',
        streetName: initialData?.address?.streetName || '',
        postalCode: initialData?.address?.postalCode || '',
      },
    },
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 max-w-4xl">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* General Information */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">General Information</h3>
            
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.name}</FormLabel>
                  <FormControl>
                    <Input placeholder="Building A" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="internalCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.internalCode}</FormLabel>
                  <FormControl>
                    <Input placeholder="BLD-001" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="buildingType"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.buildingType}</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select type" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {Object.values(BuildingType).map((type) => (
                        <SelectItem key={type} value={type}>
                          {type}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="totalFloors"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.totalFloors}</FormLabel>
                  <FormControl>
                    <Input type="number" {...field} onChange={e => field.onChange(parseInt(e.target.value, 10))} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="constructionYear"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.constructionYear}</FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      {...field} 
                      value={field.value || ''}
                      onChange={e => field.onChange(e.target.value ? parseInt(e.target.value, 10) : undefined)} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            
            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="gpsLatitude"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t.gpsLatitude}</FormLabel>
                    <FormControl>
                      <Input 
                        type="number" 
                        step="0.000001"
                        {...field} 
                        value={field.value || ''}
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
                    <FormLabel>{t.gpsLongitude}</FormLabel>
                    <FormControl>
                      <Input 
                        type="number" 
                        step="0.000001"
                        {...field} 
                        value={field.value || ''}
                        onChange={e => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)} 
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
          </div>

          {/* Address Information */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">Location & Address</h3>

            <FormField
              control={form.control}
              name="address.governorate"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.governorate}</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select governorate" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {Object.values(Governorate).map((gov) => (
                        <SelectItem key={gov} value={gov}>
                          {gov}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="address.district"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.district}</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="address.area"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.area}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value || ''} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="address.streetName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.streetName}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value || ''} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="address.postalCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t.postalCode}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value || ''} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        <div className="flex justify-end gap-4 mt-6 border-t pt-6">
          <Button type="button" variant="outline" onClick={() => window.history.back()}>
            {t.cancel}
          </Button>
          <Button type="submit" disabled={isLoading}>
            {isLoading ? 'Saving...' : t.save}
          </Button>
        </div>
      </form>
    </Form>
  );
}
