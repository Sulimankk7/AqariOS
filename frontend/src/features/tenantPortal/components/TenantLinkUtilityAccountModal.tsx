import React, { useEffect, useState } from "react";
import { AlertCircle } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/app/components/ui/button";
import { Modal } from "@/shared/components/ui/Overlays";
import { useTranslation } from "@/shared/i18n";
import { useLinkMyUtilityAccount, useReplaceMyUtilityAccount } from "../hooks/useUtilityBills";
import type { TenantUtilityAccountDto, TenantUtilityTypeName } from "../types/utilityBills.types";
import {
  isValidTenantUtilityAccountNumber,
  tenantUtilityAccountNumberMaxLength,
  tenantUtilityTypeName,
  utilityBillsErrorMessage,
} from "../utils/utilityBills";

interface TenantLinkUtilityAccountModalProps {
  open: boolean;
  onClose: () => void;
  initialUtilityType?: TenantUtilityTypeName;
  account?: TenantUtilityAccountDto | null;
}

export function TenantLinkUtilityAccountModal({
  open,
  onClose,
  initialUtilityType = "Electricity",
  account,
}: TenantLinkUtilityAccountModalProps) {
  const { t } = useTranslation();
  const linkMutation = useLinkMyUtilityAccount();
  const replaceMutation = useReplaceMyUtilityAccount();
  const isEditing = Boolean(account);
  const mutation = isEditing ? replaceMutation : linkMutation;
  const [utilityType, setUtilityType] = useState<TenantUtilityTypeName>(initialUtilityType);
  const [accountNumber, setAccountNumber] = useState("");
  const [meterNumber, setMeterNumber] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setUtilityType(account ? tenantUtilityTypeName(account.utilityType) : initialUtilityType);
      setAccountNumber(account?.accountNumber ?? "");
      setMeterNumber(account?.meterNumber ?? "");
      setErrorMessage(null);
      mutation.reset();
    }
  }, [account, initialUtilityType, open]);

  const close = () => {
    if (!mutation.isPending) onClose();
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!accountNumber.trim() || mutation.isPending) return;

    setErrorMessage(null);
    const normalizedAccountNumber = accountNumber.trim();
    const isValid = isValidTenantUtilityAccountNumber(utilityType, normalizedAccountNumber);
    if (!isValid) {
      setErrorMessage(t(`tenant.utilityBills.validation.${utilityType}`));
      return;
    }

    try {
      if (account) {
        await replaceMutation.mutateAsync({
          id: account.id,
          request: {
            accountNumber: normalizedAccountNumber,
            meterNumber: meterNumber.trim() || null,
          },
        });
      } else {
        await linkMutation.mutateAsync({
          utilityType: utilityType === "Electricity" ? 0 : 1,
          accountNumber: normalizedAccountNumber,
          meterNumber: meterNumber.trim() || null,
        });
      }
      toast.success(t(isEditing
        ? "tenant.utilityBills.edit.success"
        : "tenant.utilityBills.link.success"));
      onClose();
    } catch (error) {
      setErrorMessage(utilityBillsErrorMessage(error, t));
    }
  };

  const fieldClass = "w-full h-10 rounded-lg border border-border bg-background px-3 text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-primary/30";

  return (
    <Modal
      isOpen={open}
      onClose={close}
      title={t(isEditing ? "tenant.utilityBills.edit.title" : "tenant.utilityBills.link.title")}
      description={t(isEditing ? "tenant.utilityBills.edit.description" : "tenant.utilityBills.link.description")}
      maxWidth="lg"
    >
      <form onSubmit={submit} className="space-y-4">
        <label className="block text-xs font-medium space-y-1.5">
          <span>{t("tenant.utilityBills.fields.utilityType")} *</span>
          <select
            className={fieldClass}
            value={utilityType}
            onChange={(event) => setUtilityType(event.target.value as TenantUtilityTypeName)}
            disabled={mutation.isPending || isEditing}
          >
            <option value="Electricity">{t("tenant.utilityBills.electricity")}</option>
            <option value="Water">{t("tenant.utilityBills.water")}</option>
          </select>
        </label>

        <label className="block text-xs font-medium space-y-1.5">
          <span>{t("tenant.utilityBills.fields.accountNumber")} *</span>
          <input
            className={fieldClass}
            value={accountNumber}
            onChange={(event) => setAccountNumber(event.target.value.replace(/\D/g, ""))}
            maxLength={tenantUtilityAccountNumberMaxLength(utilityType)}
            inputMode="numeric"
            pattern="[0-9]*"
            autoComplete="off"
            disabled={mutation.isPending}
            required
          />
        </label>

        {isEditing && (
          <div className="rounded-lg border border-info/25 bg-info-bg/40 p-3 text-xs leading-relaxed text-muted-foreground">
            {t("tenant.utilityBills.edit.historyNotice")}
          </div>
        )}

        <label className="block text-xs font-medium space-y-1.5">
          <span>{t("tenant.utilityBills.fields.meterNumberOptional")}</span>
          <input
            className={fieldClass}
            value={meterNumber}
            onChange={(event) => setMeterNumber(event.target.value)}
            maxLength={100}
            autoComplete="off"
            disabled={mutation.isPending}
          />
        </label>

        {errorMessage && (
          <div role="alert" className="flex items-start gap-2 rounded-lg border border-danger/30 bg-danger-bg/50 p-3 text-xs text-danger">
            <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
            <span>{errorMessage}</span>
          </div>
        )}

        <div className="flex justify-end gap-2 pt-3 border-t border-border">
          <Button type="button" variant="outline" size="sm" onClick={close} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" size="sm" disabled={mutation.isPending || !accountNumber.trim()}>
            {mutation.isPending
              ? t(isEditing ? "tenant.utilityBills.edit.submitting" : "tenant.utilityBills.link.submitting")
              : t(isEditing ? "tenant.utilityBills.edit.submit" : "tenant.utilityBills.link.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
