import { useMemo, useState, useEffect } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/shared/ui/select";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Skeleton } from "@/shared/ui/skeleton";
import { Switch } from "@/shared/ui/switch";
import { ErrorState } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useCreatePlatformSubscription, usePlatformCompanies, usePlatformPlans, usePlatformSubscription, usePlatformSubscriptions } from "../hooks/useSubscriptions";
import type { BillingCycle, CreateCompanySubscriptionRequest } from "../types/subscriptions.types";
import { DetailsDrawer, DetailList, SubscriptionStatusBadge } from "../components/SubscriptionPrimitives";
import { getSubscriptionError } from "../utils/subscriptionErrors";
import { DatePicker } from "@/shared/components/ui/DatePicker";

const formatDateInput = (date: Date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
const parseDateInput = (value: string) => { const [year, month, day] = value.split("-").map(Number); return new Date(year, month - 1, day); };
const getTodayYYYYMMDD = () => formatDateInput(new Date());
const addCycle = (dateStr: string, cycle: BillingCycle) => {
  if (!dateStr) return "";
  const source = parseDateInput(dateStr); const targetMonth = source.getMonth() + (cycle === "Yearly" ? 12 : 1);
  const lastDay = new Date(source.getFullYear(), targetMonth + 1, 0).getDate();
  return formatDateInput(new Date(source.getFullYear(), targetMonth, Math.min(source.getDate(), lastDay)));
};
const addDays = (dateStr: string, days: number) => { const date = parseDateInput(dateStr); date.setDate(date.getDate() + days); return formatDateInput(date); };

const blank: CreateCompanySubscriptionRequest = { companyId: "", planId: "", billingCycle: "Monthly", startDate: getTodayYYYYMMDD(), endDate: addCycle(getTodayYYYYMMDD(), "Monthly") };
const statuses = ["Trialing", "Active", "PastDue", "Suspended", "Cancelled", "Expired"];

export function PlatformSubscriptionsPage() {
  const { t, language, formatCurrency, formatDate } = useTranslation(); const { user } = useAuth();
  const [page, setPage] = useState(1); const [search, setSearch] = useState(""); const [status, setStatus] = useState("all"); const [cycle, setCycle] = useState("all"); const [selectedId, setSelectedId] = useState<string | null>(null); const [open, setOpen] = useState(false); const [form, setForm] = useState<CreateCompanySubscriptionRequest>(blank);
  const [trialEnabled, setTrialEnabled] = useState(false);
  const filters = useMemo(() => ({ page, pageSize: 20, search: search.trim() || undefined, status: status === "all" ? undefined : status, billingCycle: cycle === "all" ? undefined : cycle, sortBy: "createdAt", descending: true }), [page, search, status, cycle]);
  const query = usePlatformSubscriptions(filters); const selected = usePlatformSubscription(selectedId); const companies = usePlatformCompanies(); const activePlans = usePlatformPlans({ page: 1, pageSize: 100, isActive: true }); const create = useCreatePlatformSubscription();
  const canRead = user?.permissions?.includes("platform.subscriptions.read") === true; const canCreate = user?.permissions?.includes("platform.subscriptions.manage") === true;
  
  const selectedPlan = activePlans.data?.items.find((p) => p.id === form.planId);

  useEffect(() => {
    setForm(prev => ({ ...prev, endDate: addCycle(prev.startDate, prev.billingCycle) }));
  }, [form.startDate, form.billingCycle]);

  useEffect(() => {
    setTrialEnabled(false);
  }, [selectedPlan]);

  if (!canRead) return <ErrorState title={t("subscriptions.errors.forbidden")} />;
  const submit = async () => { 
    try { 
      let trialEndDate = undefined;
      if (trialEnabled && selectedPlan?.trialDurationDays) {
        trialEndDate = addDays(form.startDate, selectedPlan.trialDurationDays);
      }
      await create.mutateAsync({ ...form, companyId: form.companyId.trim(), trialEndDate }); 
      toast.success(t("subscriptions.createSubscriptionSuccess")); 
      setOpen(false); setForm(blank); setTrialEnabled(false);
    } catch (error) { toast.error(getSubscriptionError(error, t)); } 
  };
  const totalPages = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / 20));
  
  return <section className="space-y-5">
    <header className="flex flex-wrap items-center justify-between gap-3"><div><h1 className="text-xl font-bold tracking-tight">{t("subscriptions.platformSubscriptions")}</h1><p className="mt-0.5 text-sm text-muted-foreground">{t("subscriptions.platformSubscriptionsDescription")}</p></div>{canCreate && <Button size="sm" onClick={() => { const startDate = getTodayYYYYMMDD(); setForm({ ...blank, startDate, endDate: addCycle(startDate, "Monthly") }); setTrialEnabled(false); setOpen(true); }}><Plus className="me-1 h-3.5 w-3.5" />{t("subscriptions.createSubscription")}</Button>}</header>
    <div className="flex flex-wrap gap-2"><Input className="w-full sm:max-w-xs" placeholder={t("subscriptions.searchSubscriptions")} value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} /><Select value={status} onValueChange={(value) => { setStatus(value); setPage(1); }}><SelectTrigger className="w-full sm:w-40"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">{t("subscriptions.allStatuses")}</SelectItem>{statuses.map((value) => <SelectItem key={value} value={value}>{t(`subscriptions.status.${value}`)}</SelectItem>)}</SelectContent></Select><Select value={cycle} onValueChange={(value) => { setCycle(value); setPage(1); }}><SelectTrigger className="w-full sm:w-40"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">{t("subscriptions.allCycles")}</SelectItem><SelectItem value="Monthly">{t("subscriptions.monthly")}</SelectItem><SelectItem value="Yearly">{t("subscriptions.yearly")}</SelectItem></SelectContent></Select></div>
    {query.isLoading && <div className="space-y-2"><Skeleton className="h-12 w-full" /><Skeleton className="h-12 w-full" /><Skeleton className="h-12 w-full" /></div>}{query.isError && <ErrorState title={getSubscriptionError(query.error, t)} onRetry={() => query.refetch()} />}{query.data?.items.length === 0 && <p className="py-10 text-center text-sm text-muted-foreground">{t("subscriptions.noSubscriptions")}</p>}
    {query.data && query.data.items.length > 0 && <div className="overflow-x-auto rounded-lg border border-border bg-card"><table className="min-w-[800px] w-full text-sm"><thead className="bg-muted/40 text-xs text-muted-foreground"><tr><th className="p-3 text-start">{t("subscriptions.company")}</th><th className="p-3 text-start">{t("subscriptions.plan")}</th><th className="p-3 text-start">{t("subscriptions.billingCycle")}</th><th className="p-3 text-start">{t("subscriptions.price")}</th><th className="p-3 text-start">{t("common.status")}</th><th className="p-3 text-start">{t("subscriptions.endDate")}</th></tr></thead><tbody>{query.data.items.map((item) => <tr key={item.id} onClick={() => setSelectedId(item.id)} className="cursor-pointer border-t border-border hover:bg-muted/40"><td className="p-3 font-medium">{item.companyName}</td><td className="p-3">{language === "ar" ? item.planNameAr : item.planNameEn}</td><td className="p-3">{t(`subscriptions.${item.billingCycle.toLowerCase()}`)}</td><td className="p-3 whitespace-nowrap">{formatCurrency(item.priceAtSubscription, { currency: item.currencyAtSubscription })}</td><td className="p-3"><SubscriptionStatusBadge value={item.status} /></td><td className="p-3 whitespace-nowrap">{formatDate(item.endDate, { dateStyle: "medium" })}</td></tr>)}</tbody></table></div>}
    {totalPages > 1 && <div className="flex justify-end gap-2"><Button size="sm" variant="outline" disabled={page === 1} onClick={() => setPage(page - 1)}>{t("common.back")}</Button><Button size="sm" variant="outline" disabled={page === totalPages} onClick={() => setPage(page + 1)}>{t("common.next")}</Button></div>}
    <DetailsDrawer open={Boolean(selectedId)} title={t("subscriptions.subscriptionDetails")} onClose={() => setSelectedId(null)}>{selected.isLoading && <Skeleton className="h-52 w-full" />}{selected.isError && <ErrorState title={getSubscriptionError(selected.error, t)} onRetry={() => selected.refetch()} />}{selected.data && <DetailList items={[{ label: t("subscriptions.company"), value: selected.data.companyName }, { label: t("subscriptions.plan"), value: language === "ar" ? selected.data.planNameAr : selected.data.planNameEn }, { label: t("subscriptions.billingCycle"), value: t(`subscriptions.${selected.data.billingCycle.toLowerCase()}`) }, { label: t("subscriptions.price"), value: formatCurrency(selected.data.priceAtSubscription, { currency: selected.data.currencyAtSubscription }) }, { label: t("common.status"), value: <SubscriptionStatusBadge value={selected.data.status} /> }, { label: t("subscriptions.startDate"), value: formatDate(selected.data.startDate, { dateStyle: "medium" }) }, { label: t("subscriptions.endDate"), value: formatDate(selected.data.endDate, { dateStyle: "medium" }) }, { label: t("subscriptions.trialEnd"), value: selected.data.trialEndDate && formatDate(selected.data.trialEndDate, { dateStyle: "medium" }) }, { label: t("subscriptions.created"), value: formatDate(selected.data.createdAt, { dateStyle: "medium", timeStyle: "short" }) }, { label: t("subscriptions.updated"), value: formatDate(selected.data.updatedAt, { dateStyle: "medium", timeStyle: "short" }) }]} />}</DetailsDrawer>
    
    <Dialog open={open} onOpenChange={(value) => { if (!create.isPending) { setOpen(value); if (!value) setForm(blank); } }}><DialogContent className="sm:max-w-md"><DialogHeader><DialogTitle>{t("subscriptions.createSubscription")}</DialogTitle></DialogHeader><div className="space-y-6">
      
      <Field label={t("subscriptions.company")}>
        <Select disabled={companies.isLoading || companies.isError || companies.data?.length === 0} value={form.companyId} onValueChange={(value) => setForm({ ...form, companyId: value })}>
          <SelectTrigger><SelectValue placeholder={t("subscriptions.createSubscriptionForm.selectCompany")} /></SelectTrigger>
          <SelectContent>{companies.data?.map((company) => <SelectItem key={company.id} value={company.id}>{company.displayName}</SelectItem>)}</SelectContent>
        </Select>
        {companies.isError && <p className="text-xs text-destructive">{t("subscriptions.createSubscriptionForm.companiesLoadError")}</p>}
        {companies.data?.length === 0 && <p className="text-xs text-muted-foreground">{t("subscriptions.createSubscriptionForm.noCompanies")}</p>}
      </Field>

      <Field label={t("subscriptions.plan")}>
        <Select value={form.planId} onValueChange={(value) => setForm({ ...form, planId: value })}>
          <SelectTrigger><SelectValue placeholder={t("subscriptions.selectPlan")} /></SelectTrigger>
          <SelectContent>{activePlans.data?.items.map((plan) => <SelectItem key={plan.id} value={plan.id}>{language === "ar" ? plan.nameAr : plan.nameEn}</SelectItem>)}</SelectContent>
        </Select>
      </Field>

      {selectedPlan && (
        <div className="rounded-md border border-border bg-muted/30 p-3 text-sm" aria-label={t("subscriptions.createSubscriptionForm.selectedPlan")}>
          <div className="flex justify-between font-medium mb-2"><span>{language === "ar" ? selectedPlan.nameAr : selectedPlan.nameEn}</span><span className="text-muted-foreground text-xs" dir="ltr">{selectedPlan.nameEn}</span></div>
          <div className="grid grid-cols-2 gap-y-2 gap-x-4 text-muted-foreground">
            <div><span className="block text-xs">{t(selectedPlan.pricingModel === "PayAsYouGo" ? "subscriptions.paygPlanForm.paygMonthlyRate" : "subscriptions.monthlyPrice")}</span><span className="text-foreground">{formatCurrency(selectedPlan.pricingModel === "PayAsYouGo" ? selectedPlan.paygMonthlyUnitPrice ?? 0 : selectedPlan.monthlyPrice, { currency: selectedPlan.currency })}</span></div>
            <div><span className="block text-xs">{t(selectedPlan.pricingModel === "PayAsYouGo" ? "subscriptions.paygPlanForm.paygYearlyMonthlyRate" : "subscriptions.yearlyPrice")}</span><span className="text-foreground">{formatCurrency(selectedPlan.pricingModel === "PayAsYouGo" ? selectedPlan.paygYearlyMonthlyEquivalentUnitPrice ?? 0 : selectedPlan.yearlyPrice, { currency: selectedPlan.currency })}</span></div>
            <div><span className="block text-xs">{t("subscriptions.planForm.buildings")}</span><span className="text-foreground">{selectedPlan.maxBuildings ?? t("subscriptions.planForm.unlimited")}</span></div>
            <div><span className="block text-xs">{t("subscriptions.planForm.users")}</span><span className="text-foreground">{selectedPlan.maxUsers ?? t("subscriptions.planForm.unlimited")}</span></div>
            {selectedPlan.supportsTrial && <div className="col-span-2"><span className="block text-xs">{t("subscriptions.planForm.trial")}</span><span className="text-foreground">{selectedPlan.trialDurationDays} {t("subscriptions.planForm.days")}</span></div>}
          </div>
        </div>
      )}

      {selectedPlan && (
        <div className="space-y-4">
          <Field label={t("subscriptions.billingCycle")}><div className="grid grid-cols-2 rounded-md border border-border p-1">{(["Monthly", "Yearly"] as BillingCycle[]).map((value) => <Button key={value} type="button" size="sm" variant={form.billingCycle === value ? "default" : "ghost"} onClick={() => setForm({ ...form, billingCycle: value })}>{t(`subscriptions.${value.toLowerCase()}`)}</Button>)}</div></Field>
          
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={t("subscriptions.startDate")}><DatePicker value={form.startDate} onValueChange={(startDate) => setForm({ ...form, startDate: startDate ?? "" })} ariaLabel={t("subscriptions.startDate")} /></Field>
            <Field label={t("subscriptions.createSubscriptionForm.calculatedEnd")}><div className="flex h-10 items-center rounded-md border border-input bg-muted px-3 text-sm text-muted-foreground">{form.endDate ? formatDate(form.endDate, { dateStyle: "medium" }) : "—"}</div></Field>
          </div>

          {selectedPlan.supportsTrial && (
            <div className="flex items-center justify-between rounded-md border border-border p-3">
              <div><span className="text-sm font-medium">{t("subscriptions.createSubscriptionForm.freeTrial")}</span><p className="text-xs text-muted-foreground">{t("subscriptions.createSubscriptionForm.trialDuration").replace("{days}", String(selectedPlan.trialDurationDays ?? 0))}</p></div>
              <Switch checked={trialEnabled} onCheckedChange={setTrialEnabled} />
            </div>
          )}
        </div>
      )}
    </div>
    <DialogFooter><Button variant="outline" disabled={create.isPending} onClick={() => setOpen(false)}>{t("common.cancel")}</Button><Button disabled={create.isPending || !form.companyId.trim() || !form.planId || !form.startDate || !form.endDate} onClick={submit}>{create.isPending ? t("common.saving") : t("subscriptions.createSubscription")}</Button></DialogFooter></DialogContent></Dialog>
  </section>;
}
function Field({ label, optional, children }: { label: string; optional?: string; children: React.ReactNode }) { return <label className="grid gap-2 text-sm"><span className="font-medium">{label}{optional && <span className="ms-1 font-normal text-muted-foreground">({optional})</span>}</span>{children}</label>; }
