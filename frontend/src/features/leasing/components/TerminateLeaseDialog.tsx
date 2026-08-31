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
import { Checkbox } from '@/app/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/app/components/ui/select';
import { Textarea } from '@/app/components/ui/textarea';
import {
  terminateLeaseContractSchema,
  TerminateLeaseContractFormValues,
} from '../schemas/leasing.schema';
import { TerminationType } from '../types/leasing.types';
import { useTerminateLease } from '../hooks/useLeasing';
import { getLeasingTranslation } from '../constants/translations';
import { localizeValidationMessage } from '@/shared/utils/errorHandling';
import { Alert, AlertDescription } from '@/app/components/ui/alert';
import { extractUserFriendlyError, mapApiValidationErrors } from '@/shared/utils';
import { terminationTypeToLabel } from '../constants/leasingEnums';
import { useTranslation } from '@/shared/i18n';
import { DatePicker } from '@/shared/components/ui/DatePicker';

interface TerminateLeaseDialogProps {
  contractId: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function TerminateLeaseDialog({
  contractId,
  open,
  onOpenChange,
}: TerminateLeaseDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);
  const terminateMutation = useTerminateLease();
  const [rootError, setRootError] = React.useState<string | null>(null);

  const today = new Date().toISOString().split('T')[0];

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    watch,
    reset,
    formState: { errors },
  } = useForm<TerminateLeaseContractFormValues>({
    resolver: zodResolver(terminateLeaseContractSchema),
    defaultValues: {
      terminationType: TerminationType.NormalExpiration,
      terminationDate: today,
      outstandingBalance: 0,
      depositReturnedAmount: 0,
      depositDeductionAmount: 0,
      depositDeductionReason: '',
      finalUtilitySettlementCompleted: false,
      reason: '',
      notes: '',
    },
  });

  useEffect(() => {
    setRootError(null);
    if (open) {
      const todayStr = new Date().toISOString().split('T')[0];
      reset({
        terminationType: TerminationType.NormalExpiration,
        terminationDate: todayStr,
        outstandingBalance: 0,
        depositReturnedAmount: 0,
        depositDeductionAmount: 0,
        depositDeductionReason: '',
        finalUtilitySettlementCompleted: false,
        reason: '',
        notes: '',
      });
    }
  }, [open, reset]);

  const selectedType = watch('terminationType');
  const settlementCompleted = watch('finalUtilitySettlementCompleted');

  const onSubmit = (data: TerminateLeaseContractFormValues) => {
    if (!contractId) return;
    setRootError(null);
    terminateMutation.mutate(
      { id: contractId, data },
      {
        onSuccess: () => {
          reset();
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

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('terminateTitle')}</DialogTitle>
          <DialogDescription>{t('terminateDesc')}</DialogDescription>
        </DialogHeader>

        {rootError && (
          <Alert variant="destructive">
            <AlertDescription>{rootError}</AlertDescription>
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
          <div className="space-y-2">
            <Label htmlFor="terminationType">{t('terminationType')} *</Label>
            <Select
              value={selectedType?.toString()}
              onValueChange={(val) => setValue('terminationType', Number(val) as TerminationType)}
            >
              <SelectTrigger id="terminationType" aria-required="true">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={TerminationType.NormalExpiration.toString()}>
                  {terminationTypeToLabel(TerminationType.NormalExpiration, t)}
                </SelectItem>
                <SelectItem value={TerminationType.EarlyTermination.toString()}>
                  {terminationTypeToLabel(TerminationType.EarlyTermination, t)}
                </SelectItem>
                <SelectItem value={TerminationType.MutualAgreement.toString()}>
                  {terminationTypeToLabel(TerminationType.MutualAgreement, t)}
                </SelectItem>
                <SelectItem value={TerminationType.TenantRequest.toString()}>
                  {terminationTypeToLabel(TerminationType.TenantRequest, t)}
                </SelectItem>
                <SelectItem value={TerminationType.OwnerRequest.toString()}>
                  {terminationTypeToLabel(TerminationType.OwnerRequest, t)}
                </SelectItem>
                <SelectItem value={TerminationType.LegalEviction.toString()}>
                  {terminationTypeToLabel(TerminationType.LegalEviction, t)}
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="terminationDate">{t('terminationDate')} *</Label>
            <DatePicker
              id="terminationDate"
              value={watch('terminationDate')}
              onValueChange={(value) => setValue('terminationDate', value ?? '', { shouldValidate: true })}
              ariaLabel={t('terminationDate')}
              required
              ariaInvalid={!!errors.terminationDate}
              ariaDescribedBy={errors.terminationDate ? 'term-date-error' : undefined}
            />
            {errors.terminationDate && (
              <p id="term-date-error" role="alert" className="text-xs text-destructive">
                {localizeValidationMessage(errors.terminationDate.message)}
              </p>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="outstandingBalance">{t('outstandingBalance')}</Label>
              <Input
                id="outstandingBalance"
                type="number"
                step="0.01"
                placeholder={t('zeroAmountPlaceholder')}
                {...register('outstandingBalance')}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="depositReturnedAmount">{t('depositReturned')}</Label>
              <Input
                id="depositReturnedAmount"
                type="number"
                step="0.01"
                placeholder={t('amountPlaceholder')}
                {...register('depositReturnedAmount')}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="depositDeductionAmount">{t('depositDeduction')}</Label>
              <Input
                id="depositDeductionAmount"
                type="number"
                step="0.01"
                placeholder={t('smallAmountPlaceholder')}
                {...register('depositDeductionAmount')}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="depositDeductionReason">{t('depositDeductionReason')}</Label>
            <Input
              id="depositDeductionReason"
              placeholder={t('deductionReasonPlaceholder')}
              {...register('depositDeductionReason')}
            />
          </div>

          <div className="flex items-center space-x-2 rtl:space-x-reverse pt-2">
            <Checkbox
              id="finalUtilitySettlementCompleted"
              checked={settlementCompleted}
              onCheckedChange={(checked) =>
                setValue('finalUtilitySettlementCompleted', checked === true)
              }
            />
            <Label htmlFor="finalUtilitySettlementCompleted" className="cursor-pointer">
              {t('utilitySettlement')}
            </Label>
          </div>

          <div className="space-y-2">
            <Label htmlFor="reason">{t('reason')}</Label>
            <Input id="reason" placeholder={t('summaryReasonPlaceholder')} {...register('reason')} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="notes">{t('notes')}</Label>
            <Textarea id="notes" rows={3} placeholder={t('additionalNotesPlaceholder')} {...register('notes')} />
          </div>

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={terminateMutation.isPending}
            >
              {t('cancel')}
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={terminateMutation.isPending}
              aria-busy={terminateMutation.isPending}
            >
              {terminateMutation.isPending ? t('submit') : t('terminate')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
