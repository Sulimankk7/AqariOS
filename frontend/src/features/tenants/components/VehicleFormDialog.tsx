import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/app/components/ui/dialog';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Alert, AlertDescription } from '@/app/components/ui/alert';
import { TenantVehicleDto } from '../types/tenants.types';
import { vehicleSchema, VehicleFormData } from '../schemas/vehicles.schema';
import { useCreateVehicle, useUpdateVehicle } from '../hooks/useVehicles';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';

interface VehicleFormDialogProps {
  tenantId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: TenantVehicleDto | null;
}

export function VehicleFormDialog({
  tenantId,
  open,
  onOpenChange,
  initialData = null,
}: VehicleFormDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  const isEditing = !!initialData;
  const createMutation = useCreateVehicle(tenantId);
  const updateMutation = useUpdateVehicle(tenantId);
  const [rootError, setRootError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<VehicleFormData>({
    resolver: zodResolver(vehicleSchema),
    defaultValues: {
      plateNumber: '',
      makeModel: '',
      color: '',
    },
  });

  useEffect(() => {
    if (open) {
      setRootError(null);
      if (initialData) {
        reset({
          plateNumber: initialData.plateNumber || '',
          makeModel: initialData.makeModel || '',
          color: initialData.color || '',
        });
      } else {
        reset({
          plateNumber: '',
          makeModel: '',
          color: '',
        });
      }
    }
  }, [open, initialData, reset]);

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (data: VehicleFormData) => {
    setRootError(null);
    const payload = {
      plateNumber: data.plateNumber.trim(),
      makeModel: data.makeModel.trim(),
      color: data.color.trim(),
    };

    if (isEditing && initialData) {
      updateMutation.mutate(
        { vehicleId: initialData.id, data: payload },
        {
          onSuccess: () => {
            onOpenChange(false);
          },
          onError: (err: any) => {
            if (err?.validationErrors && Object.keys(err.validationErrors).length > 0) {
              Object.entries(err.validationErrors).forEach(([field, messages]) => {
                const fieldName = field.toLowerCase() as keyof VehicleFormData;
                const messageList = messages as string[];
                if (fieldName in data && messageList.length > 0) {
                  setError(fieldName, { message: messageList[0] });
                }
              });
            } else {
              setRootError(extractUserFriendlyError(err, t('loadError')));
            }
          },
        }
      );
    } else {
      createMutation.mutate(payload, {
        onSuccess: () => {
          onOpenChange(false);
        },
        onError: (err: any) => {
          if (err?.validationErrors && Object.keys(err.validationErrors).length > 0) {
            Object.entries(err.validationErrors).forEach(([field, messages]) => {
              const fieldName = field.toLowerCase() as keyof VehicleFormData;
              const messageList = messages as string[];
              if (fieldName in data && messageList.length > 0) {
                setError(fieldName, { message: messageList[0] });
              }
            });
          } else {
            setRootError(extractUserFriendlyError(err, t('loadError')));
          }
        },
      });
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? t('editVehicle') : t('addVehicle')}</DialogTitle>
          <DialogDescription>
            {isEditing ? t('editTenantDesc') : t('createTenantDesc')}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
          {rootError && (
            <Alert variant="destructive" role="alert">
              <AlertDescription>{rootError}</AlertDescription>
            </Alert>
          )}

          {/* Plate Number Field */}
          <div className="space-y-1.5">
            <Label htmlFor="vehiclePlateNumber">
              {t('vehiclePlateNumber')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="vehiclePlateNumber"
              placeholder="12-34567"
              aria-invalid={!!errors.plateNumber}
              aria-describedby={errors.plateNumber ? 'vehiclePlateNumber-error' : undefined}
              disabled={isSubmitting}
              {...register('plateNumber')}
            />
            {errors.plateNumber && (
              <p id="vehiclePlateNumber-error" className="text-xs text-destructive">
                {t(errors.plateNumber.message || 'plateNumberRequired')}
              </p>
            )}
          </div>

          {/* Make & Model Field */}
          <div className="space-y-1.5">
            <Label htmlFor="vehicleMakeModel">
              {t('vehicleMakeModel')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="vehicleMakeModel"
              placeholder={t('vehicleMakeModelPlaceholder')}
              aria-invalid={!!errors.makeModel}
              aria-describedby={errors.makeModel ? 'vehicleMakeModel-error' : undefined}
              disabled={isSubmitting}
              {...register('makeModel')}
            />
            {errors.makeModel && (
              <p id="vehicleMakeModel-error" className="text-xs text-destructive">
                {t(errors.makeModel.message || 'makeModelRequired')}
              </p>
            )}
          </div>

          {/* Color Field */}
          <div className="space-y-1.5">
            <Label htmlFor="vehicleColor">
              {t('vehicleColor')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="vehicleColor"
              placeholder={t('vehicleColorPlaceholder')}
              aria-invalid={!!errors.color}
              aria-describedby={errors.color ? 'vehicleColor-error' : undefined}
              disabled={isSubmitting}
              {...register('color')}
            />
            {errors.color && (
              <p id="vehicleColor-error" className="text-xs text-destructive">
                {t(errors.color.message || 'colorRequired')}
              </p>
            )}
          </div>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              {t('cancel')}
            </Button>
            <Button type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>
              {isSubmitting ? t('loading') : t('save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
