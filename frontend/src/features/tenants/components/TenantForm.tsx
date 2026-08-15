import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Card, CardContent } from '@/app/components/ui/card';
import { Alert, AlertDescription } from '@/app/components/ui/alert';
import { extractUserFriendlyError, mapApiValidationErrors } from '@/shared/utils';
import {
  createTenantSchema,
  CreateTenantFormValues,
} from '../schemas/tenants.schema';
import { TenantDto } from '../types/tenants.types';
import { useCreateTenant, useUpdateTenant } from '../hooks/useTenants';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

interface TenantFormProps {
  initialValues?: Partial<TenantDto>;
  tenantId?: string;
  isEdit?: boolean;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function TenantForm({
  initialValues,
  tenantId,
  isEdit = false,
  onSuccess,
  onCancel,
}: TenantFormProps) {
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  const createMutation = useCreateTenant();
  const updateMutation = useUpdateTenant();
  const [rootError, setRootError] = React.useState<string | null>(null);

  const isPending = createMutation.isPending || updateMutation.isPending;

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<CreateTenantFormValues>({
    resolver: zodResolver(createTenantSchema),
    defaultValues: {
      name: initialValues?.name || '',
      nationalId: initialValues?.nationalId || '',
      phone: initialValues?.phone || '',
      email: initialValues?.email || '',
      occupation: initialValues?.occupation || '',
      employer: initialValues?.employer || '',
    },
  });

  useEffect(() => {
    setRootError(null);
    if (initialValues) {
      reset({
        name: initialValues.name || '',
        nationalId: initialValues.nationalId || '',
        phone: initialValues.phone || '',
        email: initialValues.email || '',
        occupation: initialValues.occupation || '',
        employer: initialValues.employer || '',
      });
    }
  }, [initialValues, reset]);

  const handleFormSubmit = (values: CreateTenantFormValues) => {
    setRootError(null);
    if (isEdit && tenantId) {
      updateMutation.mutate(
        { id: tenantId, data: values },
        {
          onSuccess: () => {
            if (onSuccess) onSuccess();
          },
          onError: (err) => {
            const mapped = mapApiValidationErrors(err, setError);
            if (!mapped) {
              setRootError(extractUserFriendlyError(err));
            }
          },
        }
      );
    } else {
      createMutation.mutate(values, {
        onSuccess: () => {
          if (onSuccess) onSuccess();
        },
        onError: (err) => {
          const mapped = mapApiValidationErrors(err, setError);
          if (!mapped) {
            setRootError(extractUserFriendlyError(err));
          }
        },
      });
    }
  };

  return (
    <Card>
      <CardContent className="pt-6">
        {rootError && (
          <Alert variant="destructive" className="mb-6" role="alert">
            <AlertDescription>{rootError}</AlertDescription>
          </Alert>
        )}

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="name">{t('name')} *</Label>
              <Input
                id="name"
                placeholder="e.g. Ahmad Al-Mansoor"
                aria-required="true"
                aria-invalid={!!errors.name}
                aria-describedby={errors.name ? 'name-error' : undefined}
                {...register('name')}
              />
              {errors.name && (
                <p id="name-error" role="alert" className="text-xs text-destructive">
                  {errors.name.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="nationalId">{t('nationalId')} *</Label>
              <Input
                id="nationalId"
                placeholder="e.g. 9981029384"
                aria-required="true"
                aria-invalid={!!errors.nationalId}
                aria-describedby={errors.nationalId ? 'nationalId-error' : undefined}
                {...register('nationalId')}
              />
              {errors.nationalId && (
                <p id="nationalId-error" role="alert" className="text-xs text-destructive">
                  {errors.nationalId.message}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="phone">{t('phone')} *</Label>
              <Input
                id="phone"
                placeholder="e.g. +962 7 9123 4567"
                dir="ltr"
                aria-required="true"
                aria-invalid={!!errors.phone}
                aria-describedby={errors.phone ? 'phone-error' : undefined}
                {...register('phone')}
              />
              {errors.phone && (
                <p id="phone-error" role="alert" className="text-xs text-destructive">
                  {errors.phone.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">{t('email')} *</Label>
              <Input
                id="email"
                type="email"
                placeholder={t('emailPlaceholder')}
                dir="ltr"
                aria-required="true"
                aria-invalid={!!errors.email}
                aria-describedby={errors.email ? 'email-error' : undefined}
                {...register('email')}
              />
              {errors.email && (
                <p id="email-error" role="alert" className="text-xs text-destructive">
                  {errors.email.message}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="occupation">{t('occupation')}</Label>
              <Input
                id="occupation"
                placeholder="e.g. Software Engineer"
                aria-invalid={!!errors.occupation}
                aria-describedby={errors.occupation ? 'occupation-error' : undefined}
                {...register('occupation')}
              />
              {errors.occupation && (
                <p id="occupation-error" role="alert" className="text-xs text-destructive">
                  {errors.occupation.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="employer">{t('employer')}</Label>
              <Input
                id="employer"
                placeholder="e.g. Amman Tech Ltd."
                aria-invalid={!!errors.employer}
                aria-describedby={errors.employer ? 'employer-error' : undefined}
                {...register('employer')}
              />
              {errors.employer && (
                <p id="employer-error" role="alert" className="text-xs text-destructive">
                  {errors.employer.message}
                </p>
              )}
            </div>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            {onCancel && (
              <Button type="button" variant="outline" onClick={onCancel} disabled={isPending}>
                {t('cancel')}
              </Button>
            )}
            <Button type="submit" disabled={isPending} aria-busy={isPending}>
              {isPending ? t('submit') : isEdit ? t('save') : t('submit')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
