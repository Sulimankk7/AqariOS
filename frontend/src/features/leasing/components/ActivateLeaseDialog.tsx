import React from 'react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/app/components/ui/alert-dialog';
import { Alert, AlertDescription } from '@/app/components/ui/alert';
import { extractUserFriendlyError } from '@/shared/utils';
import { useActivateLease } from '../hooks/useLeasing';
import { getLeasingTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

interface ActivateLeaseDialogProps {
  contractId: string | null;
  contractNumber?: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ActivateLeaseDialog({
  contractId,
  contractNumber = '',
  open,
  onOpenChange,
}: ActivateLeaseDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getLeasingTranslation(key, language);
  const activateMutation = useActivateLease();
  const [rootError, setRootError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (open) {
      setRootError(null);
    }
  }, [open]);

  const handleActivate = () => {
    if (!contractId) return;
    setRootError(null);
    activateMutation.mutate(contractId, {
      onSuccess: () => {
        onOpenChange(false);
      },
      onError: (err) => {
        setRootError(extractUserFriendlyError(err));
      },
    });
  };

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('activateConfirmTitle')}</AlertDialogTitle>
          <AlertDialogDescription>
            {t('activateConfirmDesc').replace('{number}', contractNumber)}
          </AlertDialogDescription>
        </AlertDialogHeader>

        {rootError && (
          <Alert variant="destructive" className="my-2" role="alert">
            <AlertDescription>{rootError}</AlertDescription>
          </Alert>
        )}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={activateMutation.isPending}>
            {t('cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault();
              handleActivate();
            }}
            disabled={activateMutation.isPending}
            aria-busy={activateMutation.isPending}
          >
            {activateMutation.isPending ? t('submit') : t('activate')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
