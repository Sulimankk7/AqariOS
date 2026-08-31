import { useState, type ComponentType } from "react";
import { useNavigate, useParams } from "react-router";
import { ArrowLeft, Building2, FileText, Pencil, Receipt, RefreshCw, Unlink, User } from "lucide-react";
import { toast } from "sonner";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { Button } from "@/app/components/ui/button";
import { ConfirmDialog } from "@/shared/components/ui/Overlays";
import { EmptyState, ErrorState, Skeleton } from "@/shared/components/ui/Feedback";
import { useEntityLabel, useTranslation } from "@/shared/i18n";
import { extractUserFriendlyError } from "@/shared/utils";
import { ROUTES } from "@/config/routes";
import { useRequestUtilitySync, useUnlinkUtilityAccount, useUtilityAccount, useUtilityAccountBills } from "../hooks/useUtilityBills";
import { PaymentBadge, ProviderBadge, SyncBadge } from "../components/UtilityBadges";
import type { UtilityBillPaymentStatus } from "../types/utilityBills.types";
import { providerAvailabilityName, syncStatusName, utilityTypeName } from "../utils/utilityDisplay";
import { ReplaceUtilityAccountModal } from "../components/ReplaceUtilityAccountModal";

export function UtilityAccountDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { t, formatDate, formatCurrency, formatNumber } = useTranslation();
  const entityLabel = useEntityLabel();
  const [status, setStatus] = useState<UtilityBillPaymentStatus | undefined>();
  const [confirmUnlink, setConfirmUnlink] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const account = useUtilityAccount(id);
  const billsQuery = useUtilityAccountBills(id, status);
  const bills = billsQuery.data?.pages.flatMap((page) => page.items) ?? [];
  const sync = useRequestUtilitySync();
  const unlink = useUnlinkUtilityAccount();

  const syncNow = async () => {
    if (!id) return;
    try { await sync.mutateAsync(id); toast.success(t("utilityManagement.syncAccepted")); }
    catch (error) { toast.error(extractUserFriendlyError(error, t("utilityManagement.syncFailed"))); }
  };
  const unlinkNow = async () => {
    if (!id) return;
    try { await unlink.mutateAsync(id); toast.success(t("utilityManagement.unlinkSuccess")); navigate(ROUTES.utilityBills.root); }
    catch (error) { toast.error(extractUserFriendlyError(error, t("utilityManagement.unlinkFailed"))); }
  };

  if (account.isLoading) return <PageContainer title={t("utilityManagement.detailsTitle")}><div className="space-y-4">{[1, 2, 3].map((item) => <Skeleton key={item} className="h-28 w-full" />)}</div></PageContainer>;
  if (account.isError || !account.data) return <PageContainer title={t("utilityManagement.detailsTitle")}><ErrorState message={t("utilityManagement.accountLoadError")} onRetry={() => account.refetch()} /></PageContainer>;

  const data = account.data;
  const type = utilityTypeName(data.utilityType);
  const field = "h-9 rounded-lg border border-border bg-card px-3 text-xs";
  const canSync = data.isActive && providerAvailabilityName(data.providerAvailability) === "Configured" && syncStatusName(data.syncStatus) !== "Syncing";

  return <PageContainer title={`${entityLabel("utilityType", type)} · ${data.accountNumber}`} description={t("utilityManagement.detailsDescription")}>
    <div className="space-y-5">
      <div className="flex flex-wrap justify-between gap-2"><Button variant="ghost" size="sm" onClick={() => navigate(ROUTES.utilityBills.root)} className="gap-1.5"><ArrowLeft className="h-4 w-4 rtl:rotate-180" />{t("utilityManagement.back")}</Button><div className="flex flex-wrap gap-2"><Button variant="outline" size="sm" disabled={!data.isActive} onClick={() => setEditOpen(true)}><Pencil className="h-4 w-4" />{t("utilityManagement.edit")}</Button><Button variant="outline" size="sm" disabled={sync.isPending || !canSync} onClick={syncNow} className="gap-1.5"><RefreshCw className={`h-4 w-4 ${sync.isPending ? "animate-spin" : ""}`} />{t("utilityManagement.requestSync")}</Button><Button variant="destructive" size="sm" disabled={unlink.isPending || !data.isActive || syncStatusName(data.syncStatus) === "Syncing"} onClick={() => setConfirmUnlink(true)} className="gap-1.5"><Unlink className="h-4 w-4" />{t("utilityManagement.unlink")}</Button></div></div>
      {providerAvailabilityName(data.providerAvailability) !== "Configured" && <div className="rounded-xl border border-warning/40 bg-warning-bg p-4 text-xs"><p className="font-semibold">{t("utilityManagement.automaticSyncUnavailable")}</p><p className="mt-1 text-muted-foreground">{t("utilityManagement.providerUnavailableDescription")}</p></div>}
      <div className="grid grid-cols-1 gap-4 rounded-xl border border-border bg-card p-5 text-xs sm:grid-cols-2 lg:grid-cols-4">
        <Info icon={User} label={t("utilityManagement.tenant")} value={data.tenantName} /><Info icon={Building2} label={t("utilityManagement.propertyUnit")} value={`${data.buildingName} · ${data.unitNumber}`} /><Info icon={FileText} label={t("utilityManagement.leaseContract")} value={data.leaseContractNumber} /><Info icon={Receipt} label={t("utilityManagement.meterNumber")} value={data.meterNumber || "—"} />
        <div><p className="mb-1.5 text-muted-foreground">{t("utilityManagement.syncStatus")}</p><SyncBadge value={data.syncStatus} /></div><div><p className="mb-1.5 text-muted-foreground">{t("utilityManagement.providerAvailability")}</p><ProviderBadge value={data.providerAvailability} /></div><Info label={t("utilityManagement.lastSuccessfulSync")} value={data.lastSuccessfulSyncAt ? formatDate(data.lastSuccessfulSyncAt, { dateStyle: "medium" }) : "—"} /><Info label={t("utilityManagement.consecutiveFailures")} value={formatNumber(data.consecutiveFailureCount)} />
      </div>
      <div className="space-y-3"><div className="flex items-center justify-between gap-3"><h2 className="text-sm font-semibold">{t("utilityManagement.billHistory")}</h2><select className={field} value={status === undefined ? "" : String(status)} onChange={(event) => setStatus((event.target.value || undefined) as UtilityBillPaymentStatus | undefined)}><option value="">{t("utilityManagement.allPaymentStatuses")}</option>{["Paid", "Unpaid", "Unknown"].map((value) => <option key={value} value={value}>{entityLabel("utilityPaymentStatus", value)}</option>)}</select></div>
        {billsQuery.isLoading ? <div className="space-y-3">{[1, 2, 3].map((item) => <Skeleton key={item} className="h-12 w-full" />)}</div>
          : billsQuery.isError ? <ErrorState message={t("utilityManagement.billsLoadError")} onRetry={() => billsQuery.refetch()} />
          : bills.length === 0 ? <EmptyState icon={Receipt} title={t("utilityManagement.noBills")} description={t("utilityManagement.noBillsDescription")} />
          : <><div className="hidden overflow-x-auto rounded-xl border border-border bg-card md:block"><table className="w-full text-xs"><thead className="bg-secondary/50 text-muted-foreground"><tr><th className="p-3 text-start">{t("utilityManagement.billDate")}</th><th className="p-3 text-start">{t("utilityManagement.dueDate")}</th><th className="p-3 text-start">{t("utilityManagement.amount")}</th><th className="p-3 text-start">{t("utilityManagement.payment")}</th><th className="p-3 text-start">{t("utilityManagement.discovered")}</th></tr></thead><tbody className="divide-y divide-border">{bills.map((bill) => <tr key={bill.id}><td className="p-3">{formatDate(bill.billDate, { dateStyle: "medium" })}</td><td className="p-3">{bill.dueDate ? formatDate(bill.dueDate, { dateStyle: "medium" }) : "—"}</td><td className="p-3 font-mono font-semibold">{formatCurrency(bill.amount, { currency: bill.currency })}</td><td className="p-3"><PaymentBadge value={bill.paymentStatus} /></td><td className="p-3 text-muted-foreground">{formatDate(bill.discoveredAt, { dateStyle: "medium" })}</td></tr>)}</tbody></table></div>
            <div className="space-y-3 md:hidden">{bills.map((bill) => <article key={bill.id} className="space-y-2 rounded-xl border border-border bg-card p-4 text-xs"><div className="flex items-center justify-between gap-3"><p className="font-mono text-sm font-bold">{formatCurrency(bill.amount, { currency: bill.currency })}</p><PaymentBadge value={bill.paymentStatus} /></div><div className="grid grid-cols-2 gap-2 text-muted-foreground"><p>{t("utilityManagement.bill")}: <span className="text-foreground">{formatDate(bill.billDate, { dateStyle: "medium" })}</span></p><p>{t("utilityManagement.due")}: <span className="text-foreground">{bill.dueDate ? formatDate(bill.dueDate, { dateStyle: "medium" }) : "—"}</span></p></div><p className="text-muted-foreground">{t("utilityManagement.discovered")}: {formatDate(bill.discoveredAt, { dateStyle: "medium" })}</p></article>)}</div>
            {billsQuery.hasNextPage && <div className="flex justify-center"><Button variant="outline" size="sm" disabled={billsQuery.isFetchingNextPage} onClick={() => billsQuery.fetchNextPage()}>{billsQuery.isFetchingNextPage ? t("common.loadingMore") : t("common.loadMore")}</Button></div>}</>}
      </div>
    </div>
    <ReplaceUtilityAccountModal account={editOpen ? data : null} onClose={() => setEditOpen(false)} />
    <ConfirmDialog isOpen={confirmUnlink} onClose={() => setConfirmUnlink(false)} onConfirm={unlinkNow} isLoading={unlink.isPending} title={t("utilityManagement.unlinkTitle")} description={t("utilityManagement.unlinkDescription")} confirmLabel={t("utilityManagement.unlink")} />
  </PageContainer>;
}

function Info({ icon: Icon, label, value }: { icon?: ComponentType<{ className?: string }>; label: string; value: string }) {
  return <div className="min-w-0">{Icon && <Icon className="mb-1.5 h-4 w-4 text-primary" />}<p className="text-muted-foreground">{label}</p><p className="mt-1 truncate font-semibold" title={value}>{value}</p></div>;
}
