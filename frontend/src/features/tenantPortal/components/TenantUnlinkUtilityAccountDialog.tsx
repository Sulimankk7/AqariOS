import { AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/app/components/ui/button";
import { Modal } from "@/shared/components/ui/Overlays";
import { useTranslation } from "@/shared/i18n";
import { useUnlinkMyUtilityAccount } from "../hooks/useUtilityBills";
import type { TenantUtilityAccountDto } from "../types/utilityBills.types";
import { tenantUtilityTypeName, utilityBillsErrorMessage } from "../utils/utilityBills";

export function TenantUnlinkUtilityAccountDialog({
  account,
  onClose,
}: {
  account: TenantUtilityAccountDto | null;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const mutation = useUnlinkMyUtilityAccount();

  const confirm = async () => {
    if (!account || mutation.isPending) return;
    try {
      await mutation.mutateAsync(account.id);
      toast.success(t("tenant.utilityBills.unlink.success"));
      onClose();
    } catch (error) {
      toast.error(utilityBillsErrorMessage(error, t));
    }
  };

  return (
    <Modal
      isOpen={Boolean(account)}
      onClose={() => { if (!mutation.isPending) onClose(); }}
      title={account
        ? t(`tenant.utilityBills.unlink.titleByType.${tenantUtilityTypeName(account.utilityType)}`)
        : t("tenant.utilityBills.unlink.title")}
      description={t("tenant.utilityBills.unlink.description")}
      maxWidth="sm"
    >
      <div className="space-y-4">
        <div className="flex items-start gap-3 rounded-xl border border-warning/30 bg-warning-bg/50 p-4">
          <AlertTriangle className="h-5 w-5 shrink-0 text-warning" />
          <p className="text-xs leading-relaxed text-muted-foreground">
            {t("tenant.utilityBills.unlink.historyPreserved")}
          </p>
        </div>
        <div className="flex justify-end gap-2 border-t border-border pt-3">
          <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>
            {t("common.cancel")}
          </Button>
          <Button variant="destructive" size="sm" onClick={confirm} disabled={mutation.isPending}>
            {mutation.isPending
              ? t("tenant.utilityBills.unlink.submitting")
              : t("tenant.utilityBills.unlink.submit")}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
