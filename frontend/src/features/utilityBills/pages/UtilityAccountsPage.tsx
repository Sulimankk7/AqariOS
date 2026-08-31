import { useState } from "react";
import { useNavigate } from "react-router";
import { Droplets, Eye, Plus, Receipt, Zap } from "lucide-react";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { Button } from "@/app/components/ui/button";
import { EmptyState, ErrorState, Skeleton } from "@/shared/components/ui/Feedback";
import { useEntityLabel, useTranslation } from "@/shared/i18n";
import { ROUTES } from "@/config/routes";
import { useUtilityAccounts } from "../hooks/useUtilityBills";
import type { UtilityAccountFilters } from "../types/utilityBills.types";
import { utilityTypeName } from "../utils/utilityDisplay";
import { ProviderBadge, SyncBadge } from "../components/UtilityBadges";
import { LinkUtilityAccountModal } from "../components/LinkUtilityAccountModal";

const syncStatuses = ["NeverSynced", "Syncing", "Synced", "ProviderError", "RateLimited", "Timeout", "Suspended", "InvalidAccount"] as const;

export function UtilityAccountsPage() {
  const { t, formatDate, formatCurrency } = useTranslation();
  const entityLabel = useEntityLabel();
  const navigate = useNavigate();
  const [filters, setFilters] = useState<UtilityAccountFilters>({});
  const [linkOpen, setLinkOpen] = useState(false);
  const query = useUtilityAccounts(filters);
  const accounts = query.data?.pages.flatMap((page) => page.items) ?? [];
  const field = "h-9 rounded-lg border border-border bg-card px-3 text-xs text-foreground";

  return <PageContainer title={t("utilityManagement.title")} description={t("utilityManagement.description")}>
    <div className="space-y-4">
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
        <div className="flex flex-wrap gap-2">
          <select aria-label={t("utilityManagement.utilityType")} className={field} value={filters.utilityType ?? ""} onChange={(event) => setFilters((value) => ({ ...value, utilityType: (event.target.value || undefined) as UtilityAccountFilters["utilityType"] }))}>
            <option value="">{t("utilityManagement.allUtilities")}</option>
            <option value="Electricity">{entityLabel("utilityType", "Electricity")}</option>
            <option value="Water">{entityLabel("utilityType", "Water")}</option>
          </select>
          <select aria-label={t("utilityManagement.syncStatus")} className={field} value={filters.syncStatus ?? ""} onChange={(event) => setFilters((value) => ({ ...value, syncStatus: (event.target.value || undefined) as UtilityAccountFilters["syncStatus"] }))}>
            <option value="">{t("utilityManagement.allSyncStatuses")}</option>
            {syncStatuses.map((status) => <option key={status} value={status}>{entityLabel("utilitySyncStatus", status)}</option>)}
          </select>
          <select aria-label={t("utilityManagement.accountState")} className={field} value={filters.isActive === undefined ? "" : String(filters.isActive)} onChange={(event) => setFilters((value) => ({ ...value, isActive: event.target.value === "" ? undefined : event.target.value === "true" }))}>
            <option value="">{t("utilityManagement.allStates")}</option><option value="true">{t("utilityManagement.active")}</option><option value="false">{t("utilityManagement.inactive")}</option>
          </select>
          <label className="flex h-9 items-center gap-2 rounded-lg border border-border bg-card px-3 text-xs"><input type="checkbox" checked={Boolean(filters.includeUnlinked)} onChange={(event) => setFilters((value) => ({ ...value, includeUnlinked: event.target.checked || undefined }))} />{t("utilityManagement.includeUnlinked")}</label>
          {Object.values(filters).some((value) => value !== undefined) && <Button variant="ghost" size="sm" onClick={() => setFilters({})}>{t("utilityManagement.clearFilters")}</Button>}
        </div>
        <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setLinkOpen(true)}><Plus className="h-4 w-4" />{t("utilityManagement.linkAccount")}</Button>
      </div>
      {query.isLoading ? <div className="space-y-4 rounded-xl border border-border bg-card p-5">{[1, 2, 3, 4, 5].map((item) => <Skeleton key={item} className="h-10 w-full" />)}</div>
        : query.isError ? <ErrorState message={t("utilityManagement.accountsLoadError")} onRetry={() => query.refetch()} />
        : accounts.length === 0 ? <EmptyState icon={Receipt} title={t("utilityManagement.emptyAccounts")} description={t("utilityManagement.emptyAccountsDescription")} action={<Button variant="outline" size="sm" onClick={() => setLinkOpen(true)}>{t("utilityManagement.linkFirstAccount")}</Button>} />
        : <>
          <div className="hidden overflow-x-auto rounded-xl border border-border bg-card shadow-xs md:block"><table className="w-full text-xs"><thead className="bg-secondary/50 text-muted-foreground"><tr><th className="p-3 text-start">{t("utilityManagement.utilityAndAccount")}</th><th className="p-3 text-start">{t("utilityManagement.tenantAndProperty")}</th><th className="p-3 text-start">{t("utilityManagement.sync")}</th><th className="p-3 text-start">{t("utilityManagement.provider")}</th><th className="p-3 text-start">{t("utilityManagement.latestBill")}</th><th className="p-3 text-end">{t("utilityManagement.action")}</th></tr></thead><tbody className="divide-y divide-border">{accounts.map((account) => {
            const type = utilityTypeName(account.utilityType); const Icon = type === "Water" ? Droplets : Zap;
            return <tr key={account.id} className="cursor-pointer hover:bg-secondary/30" onClick={() => navigate(ROUTES.utilityBills.details(account.id))}><td className="p-3"><div className="flex items-center gap-2"><Icon className={type === "Water" ? "h-4 w-4 text-sky-500" : "h-4 w-4 text-amber-500"} /><div><p className="font-semibold">{entityLabel("utilityType", type)}</p><p className="font-mono text-muted-foreground" dir="ltr">{account.accountNumber}</p></div></div></td><td className="p-3"><p className="font-medium">{account.tenantName}</p><p className="text-muted-foreground">{account.buildingName} · {account.unitNumber}</p></td><td className="p-3"><SyncBadge value={account.syncStatus} /></td><td className="p-3"><ProviderBadge value={account.providerAvailability} /></td><td className="p-3">{account.latestBill ? <><p className="font-mono font-semibold">{formatCurrency(account.latestBill.amount, { currency: account.latestBill.currency })}</p><p className="text-muted-foreground">{formatDate(account.latestBill.billDate, { dateStyle: "medium" })}</p></> : "—"}</td><td className="p-3 text-end"><Button variant="ghost" size="icon" aria-label={t("utilityManagement.viewDetails")}><Eye className="h-4 w-4" /></Button></td></tr>;
          })}</tbody></table></div>
          <div className="space-y-3 md:hidden">{accounts.map((account) => { const type = utilityTypeName(account.utilityType); return <button key={account.id} onClick={() => navigate(ROUTES.utilityBills.details(account.id))} className="w-full space-y-3 rounded-xl border border-border bg-card p-4 text-start"><div className="flex justify-between gap-3"><div><p className="text-sm font-semibold">{entityLabel("utilityType", type)} · <span className="font-mono" dir="ltr">{account.accountNumber}</span></p><p className="text-xs text-muted-foreground">{account.tenantName} · {account.buildingName} · {account.unitNumber}</p></div><Eye className="h-4 w-4 text-muted-foreground" /></div><div className="flex flex-wrap gap-2"><SyncBadge value={account.syncStatus} /><ProviderBadge value={account.providerAvailability} /></div></button>; })}</div>
          {query.hasNextPage && <div className="flex justify-center"><Button variant="outline" size="sm" disabled={query.isFetchingNextPage} onClick={() => query.fetchNextPage()}>{query.isFetchingNextPage ? t("common.loadingMore") : t("common.loadMore")}</Button></div>}
        </>}
    </div><LinkUtilityAccountModal open={linkOpen} onClose={() => setLinkOpen(false)} />
  </PageContainer>;
}
