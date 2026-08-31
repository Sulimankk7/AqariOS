import React, { useState, useEffect } from 'react';
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
import { Badge } from '@/app/components/ui/badge';
import { useProvisionTenantAccount } from '../hooks/useTenants';
import {
  ProvisionTenantAccountResponseDto,
  TenantProvisioningContactMethod,
} from '../types/tenants.types';
import { useTranslation } from '@/shared/i18n';
import { getTenantTranslation } from '../constants/translations';
import {
  UserPlus,
  Copy,
  Check,
  Link as LinkIcon,
  Calendar,
  Info,
  AlertCircle,
  Phone,
  Mail,
} from 'lucide-react';

interface ProvisionTenantAccountModalProps {
  tenantId: string;
  tenantName: string;
  tenantPhone?: string | null;
  tenantEmail?: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ProvisionTenantAccountModal({
  tenantId,
  tenantName,
  tenantPhone,
  tenantEmail,
  open,
  onOpenChange,
}: ProvisionTenantAccountModalProps) {
  const { language, formatDate } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);

  const [contactMethod, setContactMethod] = useState<TenantProvisioningContactMethod>(
    TenantProvisioningContactMethod.Phone
  );
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [inputError, setInputError] = useState<string | null>(null);
  const [provisionResult, setProvisionResult] = useState<ProvisionTenantAccountResponseDto | null>(null);
  const [copied, setCopied] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const provisionMutation = useProvisionTenantAccount();

  useEffect(() => {
    if (open) {
      setContactMethod(TenantProvisioningContactMethod.Phone);
      setPhone(tenantPhone || '');
      setEmail(tenantEmail || '');
      setInputError(null);
      setProvisionResult(null);
      setCopied(false);
      setErrorMessage(null);
    }
  }, [open, tenantPhone, tenantEmail]);

  const getProvisionErrorMessage = (err: any): string => {
    const detail: string = err?.response?.data?.detail ?? err?.message ?? '';

    if (detail.includes('EMAIL_BELONGS_TO_ANOTHER_TENANT'))
      return t('portalEmailConflict');

    if (detail.includes('PHONE_BELONGS_TO_ANOTHER_TENANT'))
      return t('portalPhoneConflict');

    return t('portalEnableFailed');
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (contactMethod === TenantProvisioningContactMethod.Phone) {
      const trimmedPhone = phone.trim();
      if (!trimmedPhone) {
        setInputError(t('portalPhoneRequired'));
        return;
      }
      setInputError(null);

      provisionMutation.mutate(
        {
          tenantId,
          contactMethod: TenantProvisioningContactMethod.Phone,
          phone: trimmedPhone,
          email: email.trim() || null,
        },
        {
          onSuccess: (data) => setProvisionResult(data),
          onError: (err: any) => setErrorMessage(getProvisionErrorMessage(err)),
        }
      );
    } else {
      const trimmedEmail = email.trim();
      if (!trimmedEmail) {
        setInputError(t('portalEmailRequired'));
        return;
      }

      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(trimmedEmail)) {
        setInputError(t('portalEmailInvalid'));
        return;
      }

      setInputError(null);

      provisionMutation.mutate(
        {
          tenantId,
          contactMethod: TenantProvisioningContactMethod.Email,
          email: trimmedEmail,
          phone: phone.trim() || null,
        },
        {
          onSuccess: (data) => setProvisionResult(data),
          onError: (err: any) => setErrorMessage(getProvisionErrorMessage(err)),
        }
      );
    }
  };

  const activationUrl = provisionResult?.activationToken
    ? `${window.location.origin}/auth/activate?token=${encodeURIComponent(provisionResult.activationToken)}`
    : '';

  const handleCopyLink = () => {
    if (!activationUrl) return;
    navigator.clipboard.writeText(activationUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2500);
  };

  const formatExpiry = (expiresAtStr?: string) => {
    if (!expiresAtStr) return '—';
    try {
      return formatDate(expiresAtStr, { dateStyle: 'medium', timeStyle: 'short' });
    } catch {
      return expiresAtStr;
    }
  };

  const isCurrentContactEmpty =
    contactMethod === TenantProvisioningContactMethod.Phone ? !phone.trim() : !email.trim();
  const hasNoInitialContactInfo = !tenantPhone && !tenantEmail;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        {!provisionResult ? (
          /* FORM STATE */
          <form onSubmit={handleSubmit} className="space-y-5">
            <DialogHeader className="text-right rtl:text-right">
              <DialogTitle className="text-xl font-bold flex items-center gap-2">
                <UserPlus className="w-5 h-5 text-primary" />
                {t('portalEnableTitle')}
              </DialogTitle>
              <DialogDescription className="text-sm text-muted-foreground pt-1">
                {t('portalEnableDescription')}
              </DialogDescription>
            </DialogHeader>

            {hasNoInitialContactInfo && isCurrentContactEmpty && (
              <div className="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-400 text-xs flex items-start gap-2.5 leading-relaxed">
                <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                <span>
                  {t('portalNoContact')}
                </span>
              </div>
            )}

            {errorMessage && (
              <div className="p-3 rounded-xl bg-destructive/10 border border-destructive/20 text-destructive text-xs flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0" />
                <span>{errorMessage}</span>
              </div>
            )}

            {/* DELIVERY METHOD SELECTOR — controls where the activation link is sent, NOT identity lookup */}
            <div className="space-y-2">
              <Label className="text-sm font-semibold block text-right">
                {t('portalDeliveryMethod')}
              </Label>
              <p className="text-xs text-muted-foreground text-right">
                {t('portalDeliveryDescription')}
              </p>
              <div className="grid grid-cols-2 gap-2 p-1 bg-secondary/50 rounded-xl border border-border/80">
                <button
                  type="button"
                  onClick={() => {
                    setContactMethod(TenantProvisioningContactMethod.Phone);
                    setInputError(null);
                  }}
                  className={`flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-semibold transition-all ${
                    contactMethod === TenantProvisioningContactMethod.Phone
                      ? 'bg-background text-foreground shadow-sm border border-border'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                >
                  <Phone className="w-3.5 h-3.5" />
                  <span>{t('portalPhoneSms')}</span>
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setContactMethod(TenantProvisioningContactMethod.Email);
                    setInputError(null);
                  }}
                  className={`flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-semibold transition-all ${
                    contactMethod === TenantProvisioningContactMethod.Email
                      ? 'bg-background text-foreground shadow-sm border border-border'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                >
                  <Mail className="w-3.5 h-3.5" />
                  <span>{t('portalEmail')}</span>
                </button>
              </div>
            </div>

            {/* PHONE MODE INPUT */}
            {contactMethod === TenantProvisioningContactMethod.Phone ? (
              <div className="space-y-2">
                <Label htmlFor="tenant-provision-phone" className="text-sm font-medium block text-right">
                  {t('portalPhoneLabel')} <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="tenant-provision-phone"
                  type="tel"
                  dir="ltr"
                  placeholder="+962791234567"
                  value={phone}
                  onChange={(e) => {
                    setPhone(e.target.value);
                    if (inputError) setInputError(null);
                  }}
                  disabled={provisionMutation.isPending}
                  className={inputError ? 'border-destructive' : ''}
                />
                {inputError && <p className="text-xs text-destructive mt-1 text-right">{inputError}</p>}
              </div>
            ) : (
              /* EMAIL MODE INPUT */
              <div className="space-y-2">
                <Label htmlFor="tenant-provision-email" className="text-sm font-medium block text-right">
                  {t('portalEmailLabel')} <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="tenant-provision-email"
                  type="email"
                  dir="ltr"
                  placeholder={t('emailPlaceholder')}
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    if (inputError) setInputError(null);
                  }}
                  disabled={provisionMutation.isPending}
                  className={inputError ? 'border-destructive' : ''}
                />
                {inputError && <p className="text-xs text-destructive mt-1 text-right">{inputError}</p>}
              </div>
            )}

            <DialogFooter className="flex flex-col sm:flex-row gap-2 sm:justify-end">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={provisionMutation.isPending}
              >
                {t('cancel')}
              </Button>
              <Button type="submit" disabled={provisionMutation.isPending || isCurrentContactEmpty}>
                {provisionMutation.isPending ? t('portalEnabling') : t('portalEnable')}
              </Button>
            </DialogFooter>
          </form>
        ) : (
          /* SUCCESS STATE */
          <div className="space-y-5 py-1">
            <DialogHeader className="text-right rtl:text-right space-y-1">
              <div className="flex items-center gap-2">
                {contactMethod === TenantProvisioningContactMethod.Phone ? (
                  provisionResult.smsSent ? (
                    <Badge variant="outline" className="bg-emerald-500/10 text-emerald-600 border-emerald-500/30 gap-1 text-xs">
                      <Check className="w-3.5 h-3.5" />
                      {t('portalEnabledSmsSent')}
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="bg-amber-500/10 text-amber-600 border-amber-500/30 gap-1 text-xs">
                      <AlertCircle className="w-3.5 h-3.5" />
                      {t('portalAccountLinked')}
                    </Badge>
                  )
                ) : (
                  provisionResult.emailSent ? (
                    <Badge variant="outline" className="bg-emerald-500/10 text-emerald-600 border-emerald-500/30 gap-1 text-xs">
                      <Check className="w-3.5 h-3.5" />
                      {t('portalEnabledEmailSent')}
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="bg-amber-500/10 text-amber-600 border-amber-500/30 gap-1 text-xs">
                      <AlertCircle className="w-3.5 h-3.5" />
                      {t('portalAccountLinked')}
                    </Badge>
                  )
                )}
              </div>
              <DialogTitle className="text-xl font-bold pt-2">
                {t('portalEnabledTitle')}
              </DialogTitle>
              <DialogDescription className="text-sm text-muted-foreground">
                {contactMethod === TenantProvisioningContactMethod.Phone
                  ? (provisionResult.smsSent ? t('portalSmsSuccess') : t('portalSmsDeliveryFailed'))
                  : (provisionResult.emailSent ? t('portalEmailSuccess') : t('portalEmailDeliveryFailed'))}
              </DialogDescription>
            </DialogHeader>

            {/* ACTIVATION LINK DISPLAY CARD */}
            <div className="space-y-3">
              <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block text-right">
                {t('portalActivationLink')}
              </Label>
              <div className="p-3 bg-secondary/50 rounded-xl border border-border/80 space-y-2">
                <div className="flex items-center gap-2">
                  <LinkIcon className="w-4 h-4 text-primary shrink-0" />
                  <Input
                    readOnly
                    dir="ltr"
                    value={activationUrl}
                    className="font-mono text-xs bg-background text-foreground select-all h-9"
                  />
                </div>

                <div className="flex items-center justify-between pt-1 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Calendar className="w-3.5 h-3.5" />
                    {t('portalValidUntil').replace('{date}', formatExpiry(provisionResult.expiresAt))}
                  </span>
                  <Badge variant="secondary" className="text-[10px] font-medium">
                    {t('portalSingleUse')}
                  </Badge>
                </div>
              </div>

              <Button
                type="button"
                onClick={handleCopyLink}
                className="w-full gap-2 text-sm font-semibold"
                variant={copied ? 'outline' : 'default'}
              >
                {copied ? (
                  <>
                    <Check className="w-4 h-4 text-emerald-600" />
                    {t('portalLinkCopied')}
                  </>
                ) : (
                  <>
                    <Copy className="w-4 h-4" />
                    {t('portalCopyLink')}
                  </>
                )}
              </Button>
            </div>

            <div className="p-3 rounded-xl bg-blue-500/10 border border-blue-500/20 text-blue-700 dark:text-blue-400 text-xs flex items-start gap-2 leading-relaxed">
              <Info className="w-4 h-4 shrink-0 mt-0.5" />
              <span>
                {t('portalShareNote')}
              </span>
            </div>

            <DialogFooter className="pt-2">
              <Button variant="outline" className="w-full" onClick={() => onOpenChange(false)}>
                {t('close')}
              </Button>
            </DialogFooter>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
