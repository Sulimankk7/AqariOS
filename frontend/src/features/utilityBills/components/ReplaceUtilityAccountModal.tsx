import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/app/components/ui/button";
import { Modal } from "@/shared/components/ui/Overlays";
import { useTranslation } from "@/shared/i18n";
import { extractUserFriendlyError } from "@/shared/utils";
import { useReplaceUtilityAccount } from "../hooks/useUtilityBills";
import type { ManagementUtilityAccountDto } from "../types/utilityBills.types";
import { utilityTypeName } from "../utils/utilityDisplay";

export function ReplaceUtilityAccountModal({ account, onClose }: { account: ManagementUtilityAccountDto | null; onClose: () => void }) {
  const { t } = useTranslation();
  const mutation = useReplaceUtilityAccount();
  const [accountNumber, setAccountNumber] = useState("");
  const [meterNumber, setMeterNumber] = useState("");
  const [validation, setValidation] = useState<string | null>(null);
  useEffect(() => { if (account) { setAccountNumber(account.accountNumber); setMeterNumber(account.meterNumber ?? ""); setValidation(null); mutation.reset(); } }, [account]);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!account) return;
    const value = accountNumber.trim();
    const valid = utilityTypeName(account.utilityType) === "Electricity" ? /^\d{10}$/.test(value) : /^\d{1,20}$/.test(value);
    if (!valid) { setValidation(t("utilityManagement.invalidAccountNumber")); return; }
    try {
      await mutation.mutateAsync({ id: account.id, request: { accountNumber: value, meterNumber: meterNumber.trim() || null } });
      toast.success(t("utilityManagement.updateSuccess"));
      onClose();
    } catch (error) { setValidation(extractUserFriendlyError(error, t("utilityManagement.updateFailed"))); }
  };
  const field = "w-full h-10 rounded-lg border border-border bg-background px-3 text-xs";
  return <Modal isOpen={Boolean(account)} onClose={() => { if (!mutation.isPending) onClose(); }} title={t("utilityManagement.editTitle")} description={t("utilityManagement.editDescription")} maxWidth="lg">
    <form onSubmit={submit} className="space-y-4">
      <label className="block text-xs font-medium space-y-1.5"><span>{t("utilityManagement.accountNumber")} *</span><input className={field} value={accountNumber} onChange={(event) => setAccountNumber(event.target.value)} required disabled={mutation.isPending} /></label>
      <label className="block text-xs font-medium space-y-1.5"><span>{t("utilityManagement.optionalMeterNumber")}</span><input className={field} value={meterNumber} onChange={(event) => setMeterNumber(event.target.value)} disabled={mutation.isPending} /></label>
      {validation && <p role="alert" className="text-xs text-danger">{validation}</p>}
      <div className="flex justify-end gap-2 border-t border-border pt-3"><Button type="button" variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>{t("common.cancel")}</Button><Button type="submit" size="sm" disabled={mutation.isPending}>{mutation.isPending ? t("utilityManagement.saving") : t("utilityManagement.save")}</Button></div>
    </form>
  </Modal>;
}
