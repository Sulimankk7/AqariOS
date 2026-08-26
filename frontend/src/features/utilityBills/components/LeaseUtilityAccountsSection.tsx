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
  const { language } = useTranslation();
  const ar = language === "ar";
  const query = useUtilityAccounts({ leaseContractId, includeUnlinked: true });
  const accounts = query.data?.pages.flatMap((page) => page.items) ?? [];
  const sync = useRequestUtilitySync();
  const unlink = useUnlinkUtilityAccount();
  const [linkType, setLinkType] = useState<"Electricity" | "Water" | null>(null);
  const [editing, setEditing] = useState<ManagementUtilityAccountDto | null>(null);
  const [unlinking, setUnlinking] = useState<ManagementUtilityAccountDto | null>(null);
  const performSync = async (account: ManagementUtilityAccountDto) => { try { await sync.mutateAsync(account.id); toast.success(ar ? "تمت جدولة المزامنة." : "Synchronization queued."); } catch (error) { toast.error(extractUserFriendlyError(error, ar ? "تعذر طلب المزامنة." : "Unable to request synchronization.")); } };
  const performUnlink = async () => { if (!unlinking) return; try { await unlink.mutateAsync(unlinking.id); toast.success(ar ? "أُلغي الربط مع الاحتفاظ بسجل الفواتير." : "Account unlinked; bill history was preserved."); setUnlinking(null); } catch (error) { toast.error(extractUserFriendlyError(error, ar ? "تعذر إلغاء الربط." : "Unable to unlink the account.")); } };

  return <Card>
    <CardHeader><CardTitle className="text-lg">{ar ? "الخدمات والفواتير" : "Utilities and bills"}</CardTitle></CardHeader>
    <CardContent className="space-y-4">
      {query.isLoading ? <Skeleton className="h-32 w-full" /> : query.isError ? <ErrorState message={ar ? "تعذر تحميل حسابات الخدمات." : "Unable to load utility accounts."} onRetry={() => query.refetch()} /> : <div className="grid gap-4 md:grid-cols-2">{(["Electricity", "Water"] as const).map((type) => {
        const matching = accounts.filter((account) => utilityTypeName(account.utilityType) === type);
        const current = matching.find((account) => !account.unlinkedAt);
        const previous = matching.filter((account) => account.unlinkedAt);
        const Icon = type === "Electricity" ? Zap : Droplets;
        return <section key={type} className="rounded-lg border p-4 space-y-3">
          <div className="flex items-center justify-between"><span className="flex items-center gap-2 font-semibold text-sm"><Icon className="h-4 w-4 text-primary" />{type === "Electricity" ? (ar ? "الكهرباء" : "Electricity") : (ar ? "المياه" : "Water")}</span>{current && <SyncBadge value={current.syncStatus} />}</div>
          {!current ? <Button size="sm" onClick={() => setLinkType(type)}>{ar ? "ربط حساب" : "Link account"}</Button> : <>
            <div className="text-xs"><p className="text-muted-foreground">{ar ? "الاشتراك الحالي" : "Current subscription"}</p><p className="font-mono font-semibold">{current.accountNumber}</p></div>
            <div className="flex flex-wrap gap-2"><Button variant="outline" size="sm" onClick={() => setEditing(current)}><Pencil className="h-3.5 w-3.5" />{ar ? "تعديل" : "Edit"}</Button><Button variant="outline" size="sm" onClick={() => performSync(current)} disabled={sync.isPending || syncStatusName(current.syncStatus) === "Syncing"}><RefreshCw className="h-3.5 w-3.5" />{ar ? "مزامنة" : "Sync"}</Button><Button variant="destructive" size="sm" onClick={() => setUnlinking(current)}><Trash2 className="h-3.5 w-3.5" />{ar ? "إلغاء الربط" : "Unlink"}</Button></div>
          </>}
          {previous.length > 0 && <details className="border-t pt-2 text-xs"><summary className="cursor-pointer font-medium">{ar ? `الاشتراكات السابقة (${previous.length})` : `Previous subscriptions (${previous.length})`}</summary><ul className="mt-2 space-y-1 text-muted-foreground">{previous.map((account) => <li key={account.id} className="flex justify-between gap-2"><span className="font-mono">{account.accountNumber}</span><span>{account.unlinkedAt ? new Date(account.unlinkedAt).toLocaleDateString() : "—"}</span></li>)}</ul></details>}
        </section>;
      })}</div>}
    </CardContent>
    <LinkUtilityAccountModal open={Boolean(linkType)} onClose={() => setLinkType(null)} leaseContractId={leaseContractId} initialUtilityType={linkType ?? undefined} />
    <ReplaceUtilityAccountModal account={editing} onClose={() => setEditing(null)} />
    <ConfirmDialog isOpen={Boolean(unlinking)} onClose={() => setUnlinking(null)} onConfirm={performUnlink} isLoading={unlink.isPending} title={ar ? "إلغاء ربط حساب الخدمة؟" : "Unlink utility account?"} description={ar ? "ستتوقف المزامنة المستقبلية، مع الاحتفاظ بجميع الفواتير السابقة." : "Future synchronization will stop. All historical bills will be preserved."} confirmLabel={ar ? "إلغاء الربط" : "Unlink"} />
  </Card>;
}
