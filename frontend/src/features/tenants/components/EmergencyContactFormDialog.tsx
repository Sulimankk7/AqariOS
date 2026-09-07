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
import { TenantEmergencyContactDto } from '../types/tenants.types';
import { emergencyContactSchema, EmergencyContactFormData } from '../schemas/emergencyContacts.schema';
import { useCreateEmergencyContact, useUpdateEmergencyContact } from '../hooks/useEmergencyContacts';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError, localizeValidationMessage } from '@/shared/utils';

interface EmergencyContactFormDialogProps {
  tenantId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: TenantEmergencyContactDto | null;
}

export function EmergencyContactFormDialog({
  tenantId,
  open,
  onOpenChange,
  initialData = null,
}: EmergencyContactFormDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);
  const fieldError = (message: string | undefined, fallbackKey: string) => {
    const value = message || fallbackKey;
    return /^[A-Za-z][A-Za-z0-9]*$/.test(value) ? t(value) : localizeValidationMessage(value);
  };

  const isEditing = !!initialData;
  const createMutation = useCreateEmergencyContact(tenantId);
  const updateMutation = useUpdateEmergencyContact(tenantId);
  const [rootError, setRootError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<EmergencyContactFormData>({
    resolver: zodResolver(emergencyContactSchema),
    defaultValues: {
      name: '',
      relationshipType: '',
      phone: '',
    },
  });

  useEffect(() => {
    if (open) {
      setRootError(null);
      if (initialData) {
        reset({
          name: initialData.name || '',
          relationshipType: initialData.relationshipType || '',
          phone: initialData.phone || '',
        });
      } else {
        reset({
          name: '',
          relationshipType: '',
          phone: '',
        });
      }
    }
  }, [open, initialData, reset]);

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (data: EmergencyContactFormData) => {
    setRootError(null);
    const payload = {
      name: data.name.trim(),
      relationshipType: data.relationshipType.trim(),
      phone: data.phone.trim(),
    };

    if (isEditing && initialData) {
      updateMutation.mutate(
        { contactId: initialData.id, data: payload },
        {
          onSuccess: () => {
            onOpenChange(false);
          },
          onError: (err: any) => {
            if (err?.validationErrors && Object.keys(err.validationErrors).length > 0) {
              Object.entries(err.validationErrors).forEach(([field, messages]) => {
                const fieldName = `${field.charAt(0).toLowerCase()}${field.slice(1)}` as keyof EmergencyContactFormData;
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
              const fieldName = `${field.charAt(0).toLowerCase()}${field.slice(1)}` as keyof EmergencyContactFormData;
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
          <DialogTitle>{isEditing ? t('editEmergencyContact') : t('addEmergencyContact')}</DialogTitle>
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

          {/* Name Field */}
          <div className="space-y-1.5">
            <Label htmlFor="emergencyContactName">
              {t('emergencyContactName')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="emergencyContactName"
              placeholder={t('name')}
              aria-invalid={!!errors.name}
              aria-describedby={errors.name ? 'emergencyContactName-error' : undefined}
              disabled={isSubmitting}
              {...register('name')}
            />
            {errors.name && (
              <p id="emergencyContactName-error" className="text-xs text-destructive">
                {fieldError(errors.name.message, 'nameRequired')}
              </p>
            )}
          </div>

          {/* Relationship Type Field */}
          <div className="space-y-1.5">
            <Label htmlFor="emergencyContactRelationship">
              {t('emergencyContactRelationship')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="emergencyContactRelationship"
              placeholder={t('relationship')}
              aria-invalid={!!errors.relationshipType}
              aria-describedby={errors.relationshipType ? 'emergencyContactRelationship-error' : undefined}
              disabled={isSubmitting}
              {...register('relationshipType')}
            />
            {errors.relationshipType && (
              <p id="emergencyContactRelationship-error" className="text-xs text-destructive">
                {fieldError(errors.relationshipType.message, 'relationshipTypeRequired')}
              </p>
            )}
          </div>

          {/* Phone Field */}
          <div className="space-y-1.5">
            <Label htmlFor="emergencyContactPhone">
              {t('emergencyContactPhone')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="emergencyContactPhone"
              placeholder="+962791234567"
              aria-invalid={!!errors.phone}
              aria-describedby={errors.phone ? 'emergencyContactPhone-error' : undefined}
              disabled={isSubmitting}
              {...register('phone')}
            />
            {errors.phone && (
              <p id="emergencyContactPhone-error" className="text-xs text-destructive">
                {fieldError(errors.phone.message, 'phoneRequired')}
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
