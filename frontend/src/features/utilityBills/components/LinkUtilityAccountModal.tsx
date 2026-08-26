import React, { useEffect, useState } from "react";
import { Modal } from "@/shared/components/ui/Overlays";
import { Button } from "@/app/components/ui/button";
import { useSearchLeases } from "@/features/leasing/hooks/useLeasing";
import { ContractStatus } from "@/features/leasing/types/leasing.types";
import { useLinkUtilityAccount } from "../hooks/useUtilityBills";
import { extractUserFriendlyError } from "@/shared/utils";
import { toast } from "sonner";
import { useTranslation } from "@/shared/i18n";

export function LinkUtilityAccountModal({ open, onClose, leaseContractId: fixedLeaseContractId, initialUtilityType }: { open: boolean; onClose: () => void; leaseContractId?: string; initialUtilityType?: "Electricity" | "Water" }) {
  const { language } = useTranslation(); const ar = language === "ar";
  const leases = useSearchLeases("", 100); const mutation = useLinkUtilityAccount();
  const [leaseContractId, setLease] = useState(fixedLeaseContractId ?? ""); const [utilityType, setType] = useState<"Electricity" | "Water">(initialUtilityType ?? "Electricity"); const [accountNumber, setAccount] = useState(""); const [meterNumber, setMeter] = useState(""); const [validation, setValidation] = useState<string | null>(null);
  useEffect(() => { if (open) { setLease(fixedLeaseContractId ?? ""); setType(initialUtilityType ?? "Electricity"); setAccount(""); setMeter(""); setValidation(null); } }, [open, fixedLeaseContractId, initialUtilityType]);
  const activeLeases = (leases.data ?? []).filter(x => Number(x.status) === ContractStatus.Active);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!leaseContractId || !accountNumber.trim()) return;
    const value = accountNumber.trim();
    if (!(utilityType === "Electricity" ? /^\d{10}$/.test(value) : /^\d{1,20}$/.test(value))) { setValidation(ar ? "تحقق من صيغة رقم الاشتراك الخاصة بالمزوّد." : "Check the provider account-number format."); return; }
    try {
      await mutation.mutateAsync({ leaseContractId, utilityType: utilityType === "Electricity" ? 0 : 1, accountNumber: value, meterNumber: meterNumber.trim() || null });
      toast.success(ar ? "تم ربط الحساب ووضعت المزامنة الأولية في قائمة الانتظار." : "Account linked and initial sync queued."); onClose();
    } catch (error) { toast.error(extractUserFriendlyError(error, ar ? "تعذر ربط الحساب." : "Unable to link the account.")); }
  };
  const field = "w-full h-9 rounded-lg border border-border bg-background px-3 text-xs text-foreground";
  return <Modal isOpen={open} onClose={onClose} title={ar ? "ربط حساب خدمات" : "Link utility account"} description={ar ? "اربط حساب كهرباء أو مياه بعقد إيجار نشط." : "Attach an electricity or water account to an active lease."} maxWidth="lg">
    <form onSubmit={submit} className="space-y-4">
      {!fixedLeaseContractId && <label className="block text-xs font-medium space-y-1.5"><span>{ar ? "عقد الإيجار" : "Lease contract"} *</span><select className={field} value={leaseContractId} onChange={e => setLease(e.target.value)} required><option value="">{leases.isLoading ? (ar ? "جارٍ التحميل..." : "Loading...") : (ar ? "اختر عقداً نشطاً" : "Select an active lease")}</option>{activeLeases.map(x => <option key={x.id} value={x.id}>{x.contractNumber}</option>)}</select></label>}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <label className="block text-xs font-medium space-y-1.5"><span>{ar ? "نوع الخدمة" : "Utility type"} *</span><select className={field} value={utilityType} onChange={e => setType(e.target.value as "Electricity" | "Water")}><option value="Electricity">{ar ? "الكهرباء" : "Electricity"}</option><option value="Water">{ar ? "المياه" : "Water"}</option></select></label>
        <label className="block text-xs font-medium space-y-1.5"><span>{ar ? "رقم الحساب" : "Account number"} *</span><input className={field} value={accountNumber} onChange={e => setAccount(e.target.value)} required /></label>
      </div>
      <label className="block text-xs font-medium space-y-1.5"><span>{ar ? "رقم العداد (اختياري)" : "Meter number (optional)"}</span><input className={field} value={meterNumber} onChange={e => setMeter(e.target.value)} /></label>
      {validation && <p role="alert" className="text-xs text-danger">{validation}</p>}
      <div className="flex justify-end gap-2 pt-3 border-t border-border"><Button type="button" variant="outline" size="sm" onClick={onClose}>{ar ? "إلغاء" : "Cancel"}</Button><Button type="submit" size="sm" disabled={mutation.isPending || !leaseContractId || !accountNumber.trim()}>{mutation.isPending ? (ar ? "جارٍ الربط..." : "Linking...") : (ar ? "ربط الحساب" : "Link account")}</Button></div>
    </form>
  </Modal>;
}
