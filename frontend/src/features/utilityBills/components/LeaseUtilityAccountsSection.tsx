import { useState } from "react";
import { Droplets, Pencil, RefreshCw, Trash2, Zap } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/app/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/app/components/ui/card";
import { ConfirmDialog } from "@/shared/components/ui/Overlays";
import { ErrorState, Skeleton } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { extractUserFriendlyError } from "@/shared/utils";
import { useRequestUtilitySync, useUnlinkUtilityAccount, useUtilityAccounts } from "../hooks/useUtilityBills";
import type { ManagementUtilityAccountDto } from "../types/utilityBills.types";
import { syncStatusName, utilityTypeName } from "../utils/utilityDisplay";
import { SyncBadge } from "./UtilityBadges";
import { LinkUtilityAccountModal } from "./LinkUtilityAccountModal";
import { ReplaceUtilityAccountModal } from "./ReplaceUtilityAccountModal";

export function LeaseUtilityAccountsSection({ leaseContractId }: { leaseContractId: string }) {
  const { t, formatDate } = useTranslation();
  const query = useUtilityAccounts({ leaseContractId, includeUnlinked: true });
  const accounts = query.data?.pages.flatMap((page) => page.items) ?? [];
  const sync = useRequestUtilitySync();
  const unlink = useUnlinkUtilityAccount();
  const [linkType, setLinkType] = useState<"Electricity" | "Water" | null>(null);
  const [editing, setEditing] = useState<ManagementUtilityAccountDto | null>(null);
  const [unlinking, setUnlinking] = useState<ManagementUtilityAccountDto | null>(null);
  const performSync = async (account: ManagementUtilityAccountDto) => { try { await sync.mutateAsync(account.id); toast.success(t("utilityManagement.syncQueued")); } catch (error) { toast.error(extractUserFriendlyError(error, t("utilityManagement.syncFailed"))); } };
  const performUnlink = async () => { if (!unlinking) return; try { await unlink.mutateAsync(unlinking.id); toast.success(t("utilityManagement.unlinkSuccess")); setUnlinking(null); } catch (error) { toast.error(extractUserFriendlyError(error, t("utilityManagement.unlinkFailed"))); } };

  return <Card>
    <CardHeader><CardTitle className="text-lg">{t("utilityManagement.utilitiesAndBills")}</CardTitle></CardHeader>
    <CardContent className="space-y-4">
      {query.isLoading ? <Skeleton className="h-32 w-full" /> : query.isError ? <ErrorState message={t("utilityManagement.accountsLoadError")} onRetry={() => query.refetch()} /> : <div className="grid gap-4 md:grid-cols-2">{(["Electricity", "Water"] as const).map((type) => {
        const matching = accounts.filter((account) => utilityTypeName(account.utilityType) === type);
        const current = matching.find((account) => !account.unlinkedAt);
        const previous = matching.filter((account) => account.unlinkedAt);
        const Icon = type === "Electricity" ? Zap : Droplets;
        return <section key={type} className="rounded-lg border p-4 space-y-3">
          <div className="flex items-center justify-between"><span className="flex items-center gap-2 font-semibold text-sm"><Icon className="h-4 w-4 text-primary" />{t(`enums.utilityType.${type.toLowerCase()}`)}</span>{current && <SyncBadge value={current.syncStatus} />}</div>
          {!current ? <Button size="sm" onClick={() => setLinkType(type)}>{t("utilityManagement.linkAccount")}</Button> : <>
            <div className="text-xs"><p className="text-muted-foreground">{t("utilityManagement.currentSubscription")}</p><p className="font-mono font-semibold" dir="ltr">{current.accountNumber}</p></div>
            <div className="flex flex-wrap gap-2"><Button variant="outline" size="sm" onClick={() => setEditing(current)}><Pencil className="h-3.5 w-3.5" />{t("utilityManagement.edit")}</Button><Button variant="outline" size="sm" onClick={() => performSync(current)} disabled={sync.isPending || syncStatusName(current.syncStatus) === "Syncing"}><RefreshCw className="h-3.5 w-3.5" />{t("utilityManagement.sync")}</Button><Button variant="destructive" size="sm" onClick={() => setUnlinking(current)}><Trash2 className="h-3.5 w-3.5" />{t("utilityManagement.unlink")}</Button></div>
          </>}
          {previous.length > 0 && <details className="border-t pt-2 text-xs"><summary className="cursor-pointer font-medium">{t("utilityManagement.previousSubscriptions", { count: previous.length })}</summary><ul className="mt-2 space-y-1 text-muted-foreground">{previous.map((account) => <li key={account.id} className="flex justify-between gap-2"><span className="font-mono" dir="ltr">{account.accountNumber}</span><span>{account.unlinkedAt ? formatDate(account.unlinkedAt, { dateStyle: "medium" }) : "—"}</span></li>)}</ul></details>}
        </section>;
      })}</div>}
    </CardContent>
    <LinkUtilityAccountModal open={Boolean(linkType)} onClose={() => setLinkType(null)} leaseContractId={leaseContractId} initialUtilityType={linkType ?? undefined} />
    <ReplaceUtilityAccountModal account={editing} onClose={() => setEditing(null)} />
    <ConfirmDialog isOpen={Boolean(unlinking)} onClose={() => setUnlinking(null)} onConfirm={performUnlink} isLoading={unlink.isPending} title={t("utilityManagement.unlinkTitle")} description={t("utilityManagement.unlinkDescription")} confirmLabel={t("utilityManagement.unlink")} />
  </Card>;
}
