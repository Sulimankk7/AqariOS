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
import { TenantEmergencyContactDto } from '../types/tenants.types';
import { useDeleteEmergencyContact } from '../hooks/useEmergencyContacts';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

interface DeleteEmergencyContactDialogProps {
  tenantId: string;
  contact: TenantEmergencyContactDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function DeleteEmergencyContactDialog({
  tenantId,
  contact,
  open,
  onOpenChange,
}: DeleteEmergencyContactDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);
  const deleteMutation = useDeleteEmergencyContact(tenantId);
  const [rootError, setRootError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (open) {
      setRootError(null);
    }
  }, [open]);

  const handleDelete = () => {
    if (!contact) return;
    setRootError(null);
    deleteMutation.mutate(contact.id, {
      onSuccess: () => {
        onOpenChange(false);
      },
      onError: (err) => {
        setRootError(extractUserFriendlyError(err, t('loadError')));
      },
    });
  };

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="text-destructive">
            {t('confirmDeleteEmergencyContactTitle')}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {t('confirmDeleteEmergencyContactDesc').replace('{name}', contact?.name || '')}
          </AlertDialogDescription>
        </AlertDialogHeader>

        {rootError && (
          <Alert variant="destructive" className="my-2" role="alert">
            <AlertDescription>{rootError}</AlertDescription>
          </Alert>
        )}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending}>
            {t('cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault();
              handleDelete();
            }}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            disabled={deleteMutation.isPending}
            aria-busy={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? t('submit') : t('delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
