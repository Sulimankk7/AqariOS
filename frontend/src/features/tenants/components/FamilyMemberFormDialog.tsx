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
import { TenantFamilyMemberDto } from '../types/tenants.types';
import { familyMemberSchema, FamilyMemberFormData } from '../schemas/familyMembers.schema';
import { useCreateFamilyMember, useUpdateFamilyMember } from '../hooks/useFamilyMembers';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import { extractUserFriendlyError } from '@/shared/utils';

interface FamilyMemberFormDialogProps {
  tenantId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: TenantFamilyMemberDto | null;
}

export function FamilyMemberFormDialog({
  tenantId,
  open,
  onOpenChange,
  initialData = null,
}: FamilyMemberFormDialogProps) {
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  const isEditing = !!initialData;
  const createMutation = useCreateFamilyMember(tenantId);
  const updateMutation = useUpdateFamilyMember(tenantId);
  const [rootError, setRootError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FamilyMemberFormData>({
    resolver: zodResolver(familyMemberSchema),
    defaultValues: {
      name: '',
      relationshipType: '',
      ageBracket: '',
    },
  });

  useEffect(() => {
    if (open) {
      setRootError(null);
      if (initialData) {
        reset({
          name: initialData.name || '',
          relationshipType: initialData.relationshipType || '',
          ageBracket: initialData.ageBracket || '',
        });
      } else {
        reset({
          name: '',
          relationshipType: '',
          ageBracket: '',
        });
      }
    }
  }, [open, initialData, reset]);

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (data: FamilyMemberFormData) => {
    setRootError(null);
    const payload = {
      name: data.name.trim(),
      relationshipType: data.relationshipType.trim(),
      ageBracket: data.ageBracket ? data.ageBracket.trim() : null,
    };

    if (isEditing && initialData) {
      updateMutation.mutate(
        { familyMemberId: initialData.id, data: payload },
        {
          onSuccess: () => {
            onOpenChange(false);
          },
          onError: (err: any) => {
            if (err?.validationErrors && Object.keys(err.validationErrors).length > 0) {
              Object.entries(err.validationErrors).forEach(([field, messages]) => {
                const fieldName = field.toLowerCase() as keyof FamilyMemberFormData;
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
              const fieldName = field.toLowerCase() as keyof FamilyMemberFormData;
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
          <DialogTitle>{isEditing ? t('editFamilyMember') : t('addFamilyMember')}</DialogTitle>
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
            <Label htmlFor="familyMemberName">
              {t('familyMemberName')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="familyMemberName"
              placeholder={t('name')}
              aria-invalid={!!errors.name}
              aria-describedby={errors.name ? 'familyMemberName-error' : undefined}
              disabled={isSubmitting}
              {...register('name')}
            />
            {errors.name && (
              <p id="familyMemberName-error" className="text-xs text-destructive">
                {t(errors.name.message || 'nameRequired')}
              </p>
            )}
          </div>

          {/* Relationship Type Field */}
          <div className="space-y-1.5">
            <Label htmlFor="familyMemberRelationship">
              {t('familyMemberRelationship')} <span className="text-destructive">*</span>
            </Label>
            <Input
              id="familyMemberRelationship"
              placeholder={t('relationship')}
              aria-invalid={!!errors.relationshipType}
              aria-describedby={errors.relationshipType ? 'familyMemberRelationship-error' : undefined}
              disabled={isSubmitting}
              {...register('relationshipType')}
            />
            {errors.relationshipType && (
              <p id="familyMemberRelationship-error" className="text-xs text-destructive">
                {t(errors.relationshipType.message || 'relationshipTypeRequired')}
              </p>
            )}
          </div>

          {/* Age Bracket Field */}
          <div className="space-y-1.5">
            <Label htmlFor="familyMemberAgeBracket">{t('familyMemberAgeBracket')}</Label>
            <Input
              id="familyMemberAgeBracket"
              placeholder={t('ageBracket')}
              aria-invalid={!!errors.ageBracket}
              aria-describedby={errors.ageBracket ? 'familyMemberAgeBracket-error' : undefined}
              disabled={isSubmitting}
              {...register('ageBracket')}
            />
            {errors.ageBracket && (
              <p id="familyMemberAgeBracket-error" className="text-xs text-destructive">
                {t(errors.ageBracket.message || 'ageBracketMax30')}
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
