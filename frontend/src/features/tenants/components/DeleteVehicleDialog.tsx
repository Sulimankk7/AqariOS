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
import { TenantVehicleDto } from '../types/tenants.types';
import { useDeleteVehicle } from '../hooks/useVehicles';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';

interface DeleteVehicleDialogProps {
  tenantId: string;
  vehicle: TenantVehicleDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function DeleteVehicleDialog({
  tenantId,
  vehicle,
  open,
  onOpenChange,
}: DeleteVehicleDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);
  const deleteMutation = useDeleteVehicle(tenantId);
  const [rootError, setRootError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (open) {
      setRootError(null);
    }
  }, [open]);

  const handleDelete = () => {
    if (!vehicle) return;
    setRootError(null);
    deleteMutation.mutate(vehicle.id, {
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
            {t('confirmDeleteVehicleTitle')}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {t('confirmDeleteVehicleDesc').replace('{plateNumber}', vehicle?.plateNumber || '')}
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
