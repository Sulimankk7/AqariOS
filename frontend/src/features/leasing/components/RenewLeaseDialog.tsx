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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/app/components/ui/select';
import { Textarea } from '@/app/components/ui/textarea';
import {
  renewLeaseContractSchema,
  RenewLeaseContractFormInput,
  RenewLeaseContractFormValues,
} from '../schemas/leasing.schema';
import {
  LegalRegime,
  PaymentFrequency,
  TenantType,
  LeaseContractDto,
} from '../types/leasing.types';
import { Alert, AlertDescription } from '@/app/components/ui/alert';
import { extractUserFriendlyError, mapApiValidationErrors } from '@/shared/utils';
import { useRenewLease } from '../hooks/useLeasing';
import { getLeasingTranslation } from '../constants/translations';
import { localizeValidationMessage } from '@/shared/utils/errorHandling';
import {
  paymentFrequencyToLabel,
  legalRegimeToLabel,
  tenantTypeToLabel,
} from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import { DatePicker } from '@/shared/components/ui/DatePicker';

interface RenewLeaseDialogProps {
  contract: LeaseContractDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function RenewLeaseDialog({
  contract,
  open,
  onOpenChange,
}: RenewLeaseDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);
  const renewMutation = useRenewLease();
  const [rootError, setRootError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    watch,
    reset,
    formState: { errors },
  } = useForm<RenewLeaseContractFormInput, unknown, RenewLeaseContractFormValues>({
    resolver: zodResolver(renewLeaseContractSchema),
    defaultValues: {
      contractNumber: '',
      startDate: '',
      endDate: '',
      monthlyRentAmount: 0,
      securityDepositAmount: 0,
      paymentFrequency: PaymentFrequency.Monthly,
      paymentDueDay: 1,
      legalRegime: LegalRegime.Standard,
      tenantType: TenantType.Personal,
      notes: '',
    },
  });

  useEffect(() => {
    setRootError(null);
    if (contract) {
      // Calculate start & end date pre-fill from prior contract
      const startDateStr = contract.endDate || contract.startDate || new Date().toISOString().split('T')[0];
      let endDateStr = '';

      if (contract.startDate && contract.endDate) {
        const start = new Date(contract.startDate);
        const end = new Date(contract.endDate);
        const diffMs = end.getTime() - start.getTime();
        const newStart = new Date(startDateStr);
        const newEnd = new Date(newStart.getTime() + (diffMs > 0 ? diffMs : 365 * 24 * 3600 * 1000));
        endDateStr = newEnd.toISOString().split('T')[0];
      } else {
        const newStart = new Date(startDateStr);
        const newEnd = new Date(newStart.setFullYear(newStart.getFullYear() + 1));
        endDateStr = newEnd.toISOString().split('T')[0];
      }

      reset({
        contractNumber: '', // Only new contract number starts empty
        startDate: startDateStr,
        endDate: endDateStr,
        monthlyRentAmount: contract.monthlyRentAmount || 0,
        securityDepositAmount: contract.securityDepositAmount || 0,
        paymentFrequency: contract.paymentFrequency ?? PaymentFrequency.Monthly,
        paymentDueDay: contract.paymentDueDay || 1,
        legalRegime: contract.legalRegime ?? LegalRegime.Standard,
        tenantType: contract.tenantType ?? TenantType.Personal,
        notes: contract.notes || '',
      });
    }
  }, [contract, reset, open]);

  const selectedFrequency = watch('paymentFrequency');
  const selectedRegime = watch('legalRegime');
  const selectedTenantType = watch('tenantType');

  const onSubmit = (data: RenewLeaseContractFormValues) => {
    if (!contract?.id) return;
    setRootError(null);
    renewMutation.mutate(
      { id: contract.id, data },
      {
        onSuccess: () => {
          onOpenChange(false);
        },
        onError: (err) => {
          const mapped = mapApiValidationErrors(err, setError);
          if (!mapped) {
            setRootError(extractUserFriendlyError(err));
          }
        },
      }
    );
  };

  const currencyLabel = contract?.currency ? ` (${contract.currency})` : '';

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[650px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('renewTitle')}</DialogTitle>
          <DialogDescription>{t('renewDesc')}</DialogDescription>
        </DialogHeader>

        {rootError && (
          <Alert variant="destructive">
            <AlertDescription>{rootError}</AlertDescription>
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
          <div className="space-y-2">
            <Label htmlFor="contractNumber">{t('newContractNumber')} *</Label>
            <Input
              id="contractNumber"
              placeholder={t('contractNumberPlaceholder')}
              aria-required="true"
              aria-invalid={!!errors.contractNumber}
              aria-describedby={errors.contractNumber ? 'renew-contractNumber-error' : undefined}
              {...register('contractNumber')}
            />
            {errors.contractNumber && (
              <p id="renew-contractNumber-error" role="alert" className="text-xs text-destructive">
                {localizeValidationMessage(errors.contractNumber.message)}
              </p>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="startDate">{t('startDate')} *</Label>
              <DatePicker
                id="startDate"
                value={watch('startDate')}
                onValueChange={(value) => setValue('startDate', value ?? '', { shouldValidate: true })}
                ariaLabel={t('startDate')}
                required
                ariaInvalid={!!errors.startDate}
                ariaDescribedBy={errors.startDate ? 'renew-startDate-error' : undefined}
              />
              {errors.startDate && (
                <p id="renew-startDate-error" role="alert" className="text-xs text-destructive">
                  {localizeValidationMessage(errors.startDate.message)}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="endDate">{t('endDate')} *</Label>
              <DatePicker
                id="endDate"
                value={watch('endDate')}
                onValueChange={(value) => setValue('endDate', value ?? '', { shouldValidate: true })}
                ariaLabel={t('endDate')}
                required
                ariaInvalid={!!errors.endDate}
                ariaDescribedBy={errors.endDate ? 'renew-endDate-error' : undefined}
              />
              {errors.endDate && (
                <p id="renew-endDate-error" role="alert" className="text-xs text-destructive">
                  {localizeValidationMessage(errors.endDate.message)}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="monthlyRentAmount">{t('monthlyRent')}{currencyLabel} *</Label>
              <Input
                id="monthlyRentAmount"
                type="number"
                step="0.01"
                placeholder={t('amountPlaceholder')}
                aria-required="true"
                aria-invalid={!!errors.monthlyRentAmount}
                aria-describedby={errors.monthlyRentAmount ? 'renew-monthlyRentAmount-error' : undefined}
                {...register('monthlyRentAmount')}
              />
              {errors.monthlyRentAmount && (
                <p id="renew-monthlyRentAmount-error" role="alert" className="text-xs text-destructive">
                  {localizeValidationMessage(errors.monthlyRentAmount.message)}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="securityDepositAmount">{t('securityDeposit')}{currencyLabel} *</Label>
              <Input
                id="securityDepositAmount"
                type="number"
                step="0.01"
                placeholder={t('amountPlaceholder')}
                aria-required="true"
                aria-invalid={!!errors.securityDepositAmount}
                aria-describedby={errors.securityDepositAmount ? 'renew-securityDepositAmount-error' : undefined}
                {...register('securityDepositAmount')}
              />
              {errors.securityDepositAmount && (
                <p id="renew-securityDepositAmount-error" role="alert" className="text-xs text-destructive">
                  {localizeValidationMessage(errors.securityDepositAmount.message)}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
              <Label htmlFor="paymentDueDay">{t('paymentDueDay')} (1-28) *</Label>
              <Input
                id="paymentDueDay"
                type="number"
                min={1}
                max={28}
                placeholder={t('installmentCountPlaceholder')}
                aria-required="true"
                aria-invalid={!!errors.paymentDueDay}
                aria-describedby={errors.paymentDueDay ? 'renew-paymentDueDay-error' : undefined}
                {...register('paymentDueDay')}
              />
              {errors.paymentDueDay && (
                <p id="renew-paymentDueDay-error" role="alert" className="text-xs text-destructive">
                  {localizeValidationMessage(errors.paymentDueDay.message)}
                </p>
              )}
            </div>
          </div>

          {/* Legal Regime and Tenant Type are optional (have defaults) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
            <Textarea id="notes" rows={3} {...register('notes')} />
          </div>

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={renewMutation.isPending}
            >
              {t('cancel')}
            </Button>
            <Button type="submit" disabled={renewMutation.isPending} aria-busy={renewMutation.isPending}>
              {renewMutation.isPending ? t('submit') : t('renew')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
