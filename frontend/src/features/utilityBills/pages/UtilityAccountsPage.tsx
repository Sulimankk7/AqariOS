import React, { useState } from "react";
import { useNavigate } from "react-router";
import { Plus, Receipt, Zap, Droplets, Eye } from "lucide-react";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { Button } from "@/app/components/ui/button";
import { EmptyState, ErrorState, Skeleton } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { useUtilityAccounts } from "../hooks/useUtilityBills";
import type { UtilityAccountFilters } from "../types/utilityBills.types";
import { utilityTypeName } from "../utils/utilityDisplay";
import { ProviderBadge, SyncBadge } from "../components/UtilityBadges";
import { LinkUtilityAccountModal } from "../components/LinkUtilityAccountModal";
import { ROUTES } from "@/config/routes";

export function UtilityAccountsPage() {
  const { language, formatDate } = useTranslation(); const ar = language === "ar"; const navigate = useNavigate();
  const [filters, setFilters] = useState<UtilityAccountFilters>({}); const [linkOpen, setLinkOpen] = useState(false);
  const query = useUtilityAccounts(filters); const accounts = query.data?.pages.flatMap(x => x.items) ?? [];
  const field = "h-9 rounded-lg border border-border bg-card px-3 text-xs text-foreground";
  return <PageContainer title={ar ? "فواتير الخدمات" : "Utility Bills"} description={ar ? "إدارة حسابات الكهرباء والمياه والمزامنة وسجل الفواتير." : "Manage linked electricity and water accounts, synchronization, and bill history."}>
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2">
          <select aria-label={ar ? "نوع الخدمة" : "Utility type"} className={field} value={filters.utilityType ?? ""} onChange={e => setFilters(x => ({ ...x, utilityType: (e.target.value || undefined) as UtilityAccountFilters["utilityType"] }))}><option value="">{ar ? "كل الخدمات" : "All utilities"}</option><option value="Electricity">{ar ? "الكهرباء" : "Electricity"}</option><option value="Water">{ar ? "المياه" : "Water"}</option></select>
          <select aria-label={ar ? "حالة المزامنة" : "Sync status"} className={field} value={filters.syncStatus ?? ""} onChange={e => setFilters(x => ({ ...x, syncStatus: (e.target.value || undefined) as UtilityAccountFilters["syncStatus"] }))}><option value="">{ar ? "كل حالات المزامنة" : "All sync statuses"}</option>{["NeverSynced","Syncing","Synced","ProviderError","RateLimited","Timeout","Suspended","InvalidAccount"].map(x => <option key={x} value={x}>{x.replace(/([a-z])([A-Z])/g,"$1 $2")}</option>)}</select>
          <select aria-label={ar ? "حالة الربط" : "Account state"} className={field} value={filters.isActive === undefined ? "" : String(filters.isActive)} onChange={e => setFilters(x => ({ ...x, isActive: e.target.value === "" ? undefined : e.target.value === "true" }))}><option value="">{ar ? "الكل" : "All states"}</option><option value="true">{ar ? "نشط" : "Active"}</option><option value="false">{ar ? "غير نشط" : "Inactive"}</option></select>
          <label className="flex h-9 items-center gap-2 rounded-lg border border-border bg-card px-3 text-xs"><input type="checkbox" checked={Boolean(filters.includeUnlinked)} onChange={event => setFilters(value => ({ ...value, includeUnlinked: event.target.checked || undefined }))} />{ar ? "إظهار السجل غير المربوط" : "Show unlinked history"}</label>
          {Object.values(filters).some(v => v !== undefined) && <Button variant="ghost" size="sm" onClick={() => setFilters({})}>{ar ? "مسح الفلاتر" : "Clear filters"}</Button>}
        </div>
        <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setLinkOpen(true)}><Plus className="w-4 h-4" />{ar ? "ربط حساب" : "Link account"}</Button>
      </div>
      {query.isLoading ? <div className="rounded-xl border border-border bg-card p-5 space-y-4">{[1,2,3,4,5].map(x => <Skeleton key={x} className="h-10 w-full" />)}</div> : query.isError ? <ErrorState message={ar ? "تعذر تحميل حسابات الخدمات." : "Unable to load utility accounts."} onRetry={() => query.refetch()} /> : accounts.length === 0 ? <EmptyState icon={Receipt} title={ar ? "لا توجد حسابات خدمات" : "No utility accounts"} description={ar ? "لا توجد حسابات مطابقة للفلاتر الحالية." : "No accounts match the current filters."} action={<Button variant="outline" size="sm" onClick={() => setLinkOpen(true)}>{ar ? "ربط أول حساب" : "Link first account"}</Button>} /> : <>
        <div className="hidden md:block rounded-xl border border-border bg-card overflow-x-auto shadow-xs"><table className="w-full text-xs"><thead className="bg-secondary/50 text-muted-foreground"><tr><th className="p-3 text-start">{ar ? "الخدمة والحساب" : "Utility & account"}</th><th className="p-3 text-start">{ar ? "المستأجر والعقار" : "Tenant & property"}</th><th className="p-3 text-start">{ar ? "المزامنة" : "Sync"}</th><th className="p-3 text-start">{ar ? "المزوّد" : "Provider"}</th><th className="p-3 text-start">{ar ? "آخر فاتورة" : "Latest bill"}</th><th className="p-3 text-end">{ar ? "إجراء" : "Action"}</th></tr></thead><tbody className="divide-y divide-border">{accounts.map(a => <tr key={a.id} className="hover:bg-secondary/30 cursor-pointer" onClick={() => navigate(ROUTES.utilityBills.details(a.id))}><td className="p-3"><div className="flex items-center gap-2">{utilityTypeName(a.utilityType)==="Water"?<Droplets className="w-4 h-4 text-sky-500"/>:<Zap className="w-4 h-4 text-amber-500"/>}<div><p className="font-semibold">{utilityTypeName(a.utilityType)==="Water"?(ar?"المياه":"Water"):(ar?"الكهرباء":"Electricity")}</p><p className="font-mono text-muted-foreground">{a.accountNumber}</p></div></div></td><td className="p-3"><p className="font-medium">{a.tenantName}</p><p className="text-muted-foreground">{a.buildingName} · {a.unitNumber}</p></td><td className="p-3"><SyncBadge value={a.syncStatus}/></td><td className="p-3"><ProviderBadge value={a.providerAvailability}/></td><td className="p-3">{a.latestBill ? <><p className="font-mono font-semibold">{a.latestBill.amount.toFixed(2)} {a.latestBill.currency}</p><p className="text-muted-foreground">{formatDate(a.latestBill.billDate, language)}</p></> : "—"}</td><td className="p-3 text-end"><Button variant="ghost" size="icon" aria-label={ar?"عرض التفاصيل":"View details"}><Eye className="w-4 h-4"/></Button></td></tr>)}</tbody></table></div>
        <div className="md:hidden space-y-3">{accounts.map(a => <button key={a.id} onClick={() => navigate(ROUTES.utilityBills.details(a.id))} className="w-full text-start rounded-xl border border-border bg-card p-4 space-y-3"><div className="flex justify-between gap-3"><div><p className="font-semibold text-sm">{utilityTypeName(a.utilityType)==="Water"?(ar?"المياه":"Water"):(ar?"الكهرباء":"Electricity")} · <span className="font-mono">{a.accountNumber}</span></p><p className="text-xs text-muted-foreground">{a.tenantName} · {a.buildingName} · {a.unitNumber}</p></div><Eye className="w-4 h-4 text-muted-foreground"/></div><div className="flex flex-wrap gap-2"><SyncBadge value={a.syncStatus}/><ProviderBadge value={a.providerAvailability}/></div></button>)}</div>
        {query.hasNextPage && <div className="flex justify-center"><Button variant="outline" size="sm" disabled={query.isFetchingNextPage} onClick={() => query.fetchNextPage()}>{query.isFetchingNextPage ? (ar?"جارٍ التحميل...":"Loading...") : (ar?"تحميل المزيد":"Load more")}</Button></div>}
      </>}
    </div><LinkUtilityAccountModal open={linkOpen} onClose={() => setLinkOpen(false)} />
  </PageContainer>;
}
