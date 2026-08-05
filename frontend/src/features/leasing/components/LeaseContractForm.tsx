import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Card, CardContent } from '@/app/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/app/components/ui/select';
import { Textarea } from '@/app/components/ui/textarea';
import { Alert, AlertDescription } from '@/app/components/ui/alert';
import { extractUserFriendlyError, mapApiValidationErrors } from '@/shared/utils';
import {
  createLeaseContractSchema,
  CreateLeaseContractFormValues,
} from '../schemas/leasing.schema';
import {
  LegalRegime,
  PaymentFrequency,
  TenantType,
  LeaseContractDto,
} from '../types/leasing.types';
import { useTenantsLookup, useCreateLease, useUpdateDraftLease } from '../hooks/useLeasing';
import { useApartments } from '@/features/apartments/hooks/useApartments';
import { getLeasingTranslation } from '../constants/translations';
import {
  paymentFrequencyToLabel,
  legalRegimeToLabel,
  tenantTypeToLabel,
} from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';

interface LeaseContractFormProps {
  initialValues?: Partial<LeaseContractDto>;
  contractId?: string;
  isEdit?: boolean;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function LeaseContractForm({
  initialValues,
  contractId,
  isEdit = false,
  onSuccess,
  onCancel,
}: LeaseContractFormProps) {
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);

  const { data: tenants, isLoading: isLoadingTenants } = useTenantsLookup('');
  const { data: apartments, isLoading: isLoadingApartments } = useApartments();

  const createMutation = useCreateLease();
  const updateMutation = useUpdateDraftLease();
  const [rootError, setRootError] = React.useState<string | null>(null);

  const isPending = createMutation.isPending || updateMutation.isPending;

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    watch,
    reset,
    formState: { errors },
  } = useForm<CreateLeaseContractFormValues>({
    resolver: zodResolver(createLeaseContractSchema),
    defaultValues: {
      apartmentId: initialValues?.apartmentId || '',
      tenantId: initialValues?.tenantId || '',
      contractNumber: initialValues?.contractNumber || '',
      startDate: initialValues?.startDate ? initialValues.startDate.split('T')[0] : '',
      endDate: initialValues?.endDate ? initialValues.endDate.split('T')[0] : '',
      monthlyRentAmount: initialValues?.monthlyRentAmount || 0,
      securityDepositAmount: initialValues?.securityDepositAmount || 0,
      paymentFrequency: initialValues?.paymentFrequency ?? PaymentFrequency.Monthly,
      paymentDueDay: initialValues?.paymentDueDay || 1,
      legalRegime: initialValues?.legalRegime ?? LegalRegime.Standard,
      tenantType: initialValues?.tenantType ?? TenantType.Personal,
      notes: initialValues?.notes || '',
    },
  });

  useEffect(() => {
    setRootError(null);
    if (initialValues) {
      reset({
        apartmentId: initialValues.apartmentId || '',
        tenantId: initialValues.tenantId || '',
        contractNumber: initialValues.contractNumber || '',
        startDate: initialValues.startDate ? initialValues.startDate.split('T')[0] : '',
        endDate: initialValues.endDate ? initialValues.endDate.split('T')[0] : '',
        monthlyRentAmount: initialValues.monthlyRentAmount || 0,
        securityDepositAmount: initialValues.securityDepositAmount || 0,
        paymentFrequency: initialValues.paymentFrequency ?? PaymentFrequency.Monthly,
        paymentDueDay: initialValues.paymentDueDay || 1,
        legalRegime: initialValues.legalRegime ?? LegalRegime.Standard,
        tenantType: initialValues.tenantType ?? TenantType.Personal,
        notes: initialValues.notes || '',
      });
    }
  }, [initialValues, reset]);

  const selectedApartmentId = watch('apartmentId');
  const selectedTenantId = watch('tenantId');
  const selectedFrequency = watch('paymentFrequency');
  const selectedRegime = watch('legalRegime');
  const selectedTenantType = watch('tenantType');

  const handleFormSubmit = (values: CreateLeaseContractFormValues) => {
    setRootError(null);
    if (isEdit && contractId) {
      const { contractNumber, ...updatePayload } = values;
      updateMutation.mutate(
        { id: contractId, data: updatePayload },
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
          <Alert variant="destructive" className="mb-6">
            <AlertDescription>{rootError}</AlertDescription>
          </Alert>
        )}

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-6">
          {!isEdit && (
            <div className="space-y-2">
              <Label htmlFor="contractNumber">{t('contractNumber')} *</Label>
              <Input
                id="contractNumber"
                placeholder="e.g. LSE-2026-001"
                aria-required="true"
                aria-invalid={!!errors.contractNumber}
                aria-describedby={errors.contractNumber ? 'contractNumber-error' : undefined}
                {...register('contractNumber')}
              />
              {errors.contractNumber && (
                <p id="contractNumber-error" role="alert" className="text-xs text-destructive">
                  {errors.contractNumber.message}
                </p>
              )}
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="apartmentId">{t('apartment')} *</Label>
              <Select
                value={selectedApartmentId}
                onValueChange={(val) => setValue('apartmentId', val, { shouldValidate: true })}
                disabled={isLoadingApartments}
              >
                <SelectTrigger
                  id="apartmentId"
                  aria-required="true"
                  aria-invalid={!!errors.apartmentId}
                  aria-describedby={errors.apartmentId ? 'apartmentId-error' : undefined}
                >
                  <SelectValue placeholder={t('selectApartment')} />
                </SelectTrigger>
                <SelectContent>
                  {apartments?.map((apt) => (
                    <SelectItem key={apt.id} value={apt.id}>
                      Unit {apt.unitNumber} ({apt.areaSqm} m²)
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.apartmentId && (
                <p id="apartmentId-error" role="alert" className="text-xs text-destructive">
                  {errors.apartmentId.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="tenantId">{t('tenant')} *</Label>
              <Select
                value={selectedTenantId}
                onValueChange={(val) => setValue('tenantId', val, { shouldValidate: true })}
                disabled={isLoadingTenants}
              >
                <SelectTrigger
                  id="tenantId"
                  aria-required="true"
                  aria-invalid={!!errors.tenantId}
                  aria-describedby={errors.tenantId ? 'tenantId-error' : undefined}
                >
                  <SelectValue placeholder={t('selectTenant')} />
                </SelectTrigger>
                <SelectContent>
                  {tenants?.map((tnt) => (
                    <SelectItem key={tnt.id} value={tnt.id}>
                      {tnt.name} ({tnt.phone || tnt.nationalId || 'Tenant'})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.tenantId && (
                <p id="tenantId-error" role="alert" className="text-xs text-destructive">
                  {errors.tenantId.message}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="startDate">{t('startDate')} *</Label>
              <Input
                id="startDate"
                type="date"
                placeholder="YYYY-MM-DD"
                aria-required="true"
                aria-invalid={!!errors.startDate}
                aria-describedby={errors.startDate ? 'startDate-error' : undefined}
                {...register('startDate')}
              />
              {errors.startDate && (
                <p id="startDate-error" role="alert" className="text-xs text-destructive">
                  {errors.startDate.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="endDate">{t('endDate')} *</Label>
              <Input
                id="endDate"
                type="date"
                placeholder="YYYY-MM-DD"
                aria-required="true"
                aria-invalid={!!errors.endDate}
                aria-describedby={errors.endDate ? 'endDate-error' : undefined}
                {...register('endDate')}
              />
              {errors.endDate && (
                <p id="endDate-error" role="alert" className="text-xs text-destructive">
                  {errors.endDate.message}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="monthlyRentAmount">{t('monthlyRent')} *</Label>
              <Input
                id="monthlyRentAmount"
                type="number"
                step="0.01"
                placeholder="e.g. 250.00"
                aria-required="true"
                aria-invalid={!!errors.monthlyRentAmount}
                aria-describedby={errors.monthlyRentAmount ? 'monthlyRentAmount-error' : undefined}
                {...register('monthlyRentAmount')}
              />
              {errors.monthlyRentAmount && (
                <p id="monthlyRentAmount-error" role="alert" className="text-xs text-destructive">
                  {errors.monthlyRentAmount.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="securityDepositAmount">{t('securityDeposit')} *</Label>
              <Input
                id="securityDepositAmount"
                type="number"
                step="0.01"
                placeholder="e.g. 500.00"
                aria-required="true"
                aria-invalid={!!errors.securityDepositAmount}
                aria-describedby={errors.securityDepositAmount ? 'securityDepositAmount-error' : undefined}
                {...register('securityDepositAmount')}
              />
              {errors.securityDepositAmount && (
                <p id="securityDepositAmount-error" role="alert" className="text-xs text-destructive">
                  {errors.securityDepositAmount.message}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="paymentFrequency">{t('paymentFrequency')} *</Label>
              <Select
                value={selectedFrequency?.toString()}
                onValueChange={(val) => setValue('paymentFrequency', Number(val) as PaymentFrequency)}
              >
                <SelectTrigger id="paymentFrequency" aria-required="true">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={PaymentFrequency.Monthly.toString()}>
                    {paymentFrequencyToLabel(PaymentFrequency.Monthly, t)}
                  </SelectItem>
                  <SelectItem value={PaymentFrequency.Quarterly.toString()}>
                    {paymentFrequencyToLabel(PaymentFrequency.Quarterly, t)}
                  </SelectItem>
                  <SelectItem value={PaymentFrequency.SemiAnnual.toString()}>
                    {paymentFrequencyToLabel(PaymentFrequency.SemiAnnual, t)}
                  </SelectItem>
                  <SelectItem value={PaymentFrequency.Annual.toString()}>
                    {paymentFrequencyToLabel(PaymentFrequency.Annual, t)}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="paymentDueDay">{t('paymentDueDay')} (1-31) *</Label>
              <Input
                id="paymentDueDay"
                type="number"
                min={1}
                max={31}
                placeholder="e.g. 1"
                aria-required="true"
                aria-invalid={!!errors.paymentDueDay}
                aria-describedby={errors.paymentDueDay ? 'paymentDueDay-error' : undefined}
                {...register('paymentDueDay')}
              />
              {errors.paymentDueDay && (
                <p id="paymentDueDay-error" role="alert" className="text-xs text-destructive">
                  {errors.paymentDueDay.message}
                </p>
              )}
            </div>
          </div>

          {/* Legal Regime & Tenant Type are optional per API spec (have defaults) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="legalRegime">{t('legalRegime')}</Label>
              <Select
                value={selectedRegime?.toString()}
                onValueChange={(val) => setValue('legalRegime', Number(val) as LegalRegime)}
              >
                <SelectTrigger id="legalRegime">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={LegalRegime.Standard.toString()}>
                    {legalRegimeToLabel(LegalRegime.Standard, t)}
                  </SelectItem>
                  <SelectItem value={LegalRegime.OldRentLaw.toString()}>
                    {legalRegimeToLabel(LegalRegime.OldRentLaw, t)}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="tenantType">{t('tenantType')}</Label>
              <Select
                value={selectedTenantType?.toString()}
                onValueChange={(val) => setValue('tenantType', Number(val) as TenantType)}
              >
                <SelectTrigger id="tenantType">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={TenantType.Personal.toString()}>
                    {tenantTypeToLabel(TenantType.Personal, t)}
                  </SelectItem>
                  <SelectItem value={TenantType.Corporate.toString()}>
                    {tenantTypeToLabel(TenantType.Corporate, t)}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="notes">{t('notes')}</Label>
            <Textarea id="notes" rows={3} placeholder={t('notes')} {...register('notes')} />
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
