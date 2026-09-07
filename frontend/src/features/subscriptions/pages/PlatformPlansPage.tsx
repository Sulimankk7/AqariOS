import { useMemo, useState } from "react";
import { Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Textarea } from "@/shared/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/shared/ui/select";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Switch } from "@/shared/ui/switch";
import { Skeleton } from "@/shared/ui/skeleton";
import { ErrorState } from "@/shared/components/ui/Feedback";
import { useTranslation } from "@/shared/i18n";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ApiError } from "@/shared/lib/http";
import { extractUserFriendlyError, localizeValidationMessage } from "@/shared/utils/errorHandling";
import { useCreatePlatformPlan, usePlatformPlan, usePlatformPlans, useSetPlatformPlanActive } from "../hooks/useSubscriptions";
import type { SubscriptionPlanDto, SubscriptionPricingModel } from "../types/subscriptions.types";
import { DetailsDrawer, DetailList, SubscriptionStatusBadge } from "../components/SubscriptionPrimitives";

type PlanDraft = { nameEn: string; nameAr: string; descriptionEn: string; descriptionAr: string; pricingModel: SubscriptionPricingModel; monthlyPrice: number; yearlyPrice: number; paygMonthlyUnitPrice: number; paygYearlyMonthlyEquivalentUnitPrice: number; maxBuildings: number | null; maxUsers: number | null; supportsTrial: boolean; trialDurationDays: number | null; unlimitedBuildings: boolean; unlimitedUsers: boolean; };
type DraftField = keyof PlanDraft;
const emptyPlan: PlanDraft = { nameEn: "", nameAr: "", descriptionEn: "", descriptionAr: "", pricingModel: "Fixed", monthlyPrice: 0, yearlyPrice: 0, paygMonthlyUnitPrice: 0, paygYearlyMonthlyEquivalentUnitPrice: 0, maxBuildings: null, maxUsers: null, supportsTrial: false, trialDurationDays: null, unlimitedBuildings: true, unlimitedUsers: true };
const suggestCode = (name: string) => name.trim().toUpperCase().replace(/[^A-Z0-9]+/g, "_").replace(/^_+|_+$/g, "").replace(/_+/g, "_").slice(0, 50) || "PLAN";

export function PlatformPlansPage() {
  const { t, language, formatCurrency, formatNumber } = useTranslation(); const { user } = useAuth();
  const [page, setPage] = useState(1); const [search, setSearch] = useState(""); const [active, setActive] = useState("all"); const [selectedId, setSelectedId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false); const [draft, setDraft] = useState<PlanDraft>(emptyPlan); const [errors, setErrors] = useState<Partial<Record<DraftField, string>>>({}); const [lifecycle, setLifecycle] = useState<SubscriptionPlanDto | null>(null);
  const filters = useMemo(() => ({ page, pageSize: 20, isActive: active === "all" ? undefined : active === "active", search: search.trim() || undefined }), [page, active, search]);
  const plans = usePlatformPlans(filters); const selected = usePlatformPlan(selectedId); const create = useCreatePlatformPlan(); const setPlanActive = useSetPlatformPlanActive();
  const canRead = user?.permissions?.includes("platform.plans.read") === true; const canCreate = user?.permissions?.includes("platform.plans.create") === true; const canLifecycle = user?.permissions?.includes("platform.plans.lifecycle") === true;
  
  if (!canRead) return <ErrorState title={t("subscriptions.errors.forbidden")} />;
  const setField = <K extends keyof PlanDraft>(field: K, value: PlanDraft[K]) => { setDraft((current) => ({ ...current, [field]: value })); setErrors((current) => ({ ...current, [field]: undefined })); };
  const validate = () => { const next: Partial<Record<DraftField, string>> = {}; if (!draft.nameAr.trim()) next.nameAr = t("subscriptions.planForm.required"); if (!draft.nameEn.trim()) next.nameEn = t("subscriptions.planForm.required"); if (draft.pricingModel === "Fixed") { if (!(draft.monthlyPrice > 0)) next.monthlyPrice = t("subscriptions.planForm.positivePrice"); if (!(draft.yearlyPrice > 0)) next.yearlyPrice = t("subscriptions.planForm.positivePrice"); } else { if (!(draft.paygMonthlyUnitPrice > 0)) next.paygMonthlyUnitPrice = t("subscriptions.planForm.positivePrice"); if (!(draft.paygYearlyMonthlyEquivalentUnitPrice > 0)) next.paygYearlyMonthlyEquivalentUnitPrice = t("subscriptions.planForm.positivePrice"); } if (!draft.unlimitedBuildings && !(Number(draft.maxBuildings) > 0)) next.maxBuildings = t("subscriptions.planForm.positiveLimit"); if (!draft.unlimitedUsers && !(Number(draft.maxUsers) > 0)) next.maxUsers = t("subscriptions.planForm.positiveLimit"); if (draft.supportsTrial && !(Number(draft.trialDurationDays) > 0)) next.trialDurationDays = t("subscriptions.planForm.positiveDuration"); setErrors(next); return Object.keys(next).length === 0; };
  const createPlan = async () => { if (!validate()) return; try { await create.mutateAsync({ code: suggestCode(draft.nameEn), nameEn: draft.nameEn.trim(), nameAr: draft.nameAr.trim(), descriptionEn: draft.descriptionEn?.trim() || undefined, descriptionAr: draft.descriptionAr?.trim() || undefined, pricingModel: draft.pricingModel, monthlyPrice: draft.pricingModel === "Fixed" ? draft.monthlyPrice : 0, yearlyPrice: draft.pricingModel === "Fixed" ? draft.yearlyPrice : 0, paygMonthlyUnitPrice: draft.pricingModel === "PayAsYouGo" ? draft.paygMonthlyUnitPrice : null, paygYearlyMonthlyEquivalentUnitPrice: draft.pricingModel === "PayAsYouGo" ? draft.paygYearlyMonthlyEquivalentUnitPrice : null, currency: "JOD", maxBuildings: draft.unlimitedBuildings ? null : draft.maxBuildings, maxUsers: draft.unlimitedUsers ? null : draft.maxUsers, maxStorageMb: null, featureFlags: "{}", supportsTrial: draft.supportsTrial, trialDurationDays: draft.supportsTrial ? draft.trialDurationDays : null, sortOrder: 0 }); toast.success(t("subscriptions.createPlanSuccess")); setFormOpen(false); setDraft(emptyPlan); setErrors({}); } catch (error) { if (error instanceof ApiError && error.validationErrors) { const mapped: Partial<Record<DraftField, string>> = {}; Object.entries(error.validationErrors).forEach(([key, messages]) => { mapped[`${key.charAt(0).toLowerCase()}${key.slice(1)}` as DraftField] = messages.map(localizeValidationMessage).join(" "); }); setErrors(mapped); } toast.error(planError(error, t)); } };
  const updateLifecycle = async () => { if (!lifecycle) return; try { await setPlanActive.mutateAsync({ id: lifecycle.id, active: !lifecycle.isActive }); toast.success(t("subscriptions.planUpdated")); setLifecycle(null); setSelectedId(null); } catch (error) { toast.error(planError(error, t)); } };
  const totalPages = Math.max(1, Math.ceil((plans.data?.totalCount ?? 0) / 20));
  return <section className="space-y-5">
    <header className="flex flex-wrap items-center justify-between gap-3"><div><h1 className="text-xl font-bold tracking-tight">{t("subscriptions.platformPlans")}</h1><p className="mt-0.5 text-sm text-muted-foreground">{t("subscriptions.platformPlansDescription")}</p></div><div className="flex gap-2"><Button variant="outline" size="sm" onClick={() => plans.refetch()} disabled={plans.isFetching}><RefreshCw className={`me-1 h-3.5 w-3.5 ${plans.isFetching ? "animate-spin" : ""}`} />{t("subscriptions.refresh")}</Button>{canCreate && <Button size="sm" onClick={() => setFormOpen(true)}><Plus className="me-1 h-3.5 w-3.5" />{t("subscriptions.createPlan")}</Button>}</div></header>
    <div className="flex flex-wrap gap-2"><Input className="w-full sm:max-w-xs" placeholder={t("subscriptions.searchPlans")} value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} /><Select value={active} onValueChange={(value) => { setActive(value); setPage(1); }}><SelectTrigger className="w-full sm:w-40"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">{t("subscriptions.planForm.allPlanStatuses")}</SelectItem><SelectItem value="active">{t("subscriptions.active")}</SelectItem><SelectItem value="inactive">{t("subscriptions.inactive")}</SelectItem></SelectContent></Select></div>
    {plans.isLoading && <div className="space-y-2"><Skeleton className="h-12 w-full" /><Skeleton className="h-12 w-full" /><Skeleton className="h-12 w-full" /></div>}{plans.isError && <ErrorState title={planError(plans.error, t)} onRetry={() => plans.refetch()} />}{plans.data?.items.length === 0 && <p className="py-10 text-center text-sm text-muted-foreground">{t("subscriptions.noPlans")}</p>}
    {plans.data && plans.data.items.length > 0 && <div className="overflow-x-auto rounded-lg border border-border bg-card"><table className="min-w-[760px] w-full text-sm"><thead className="bg-muted/40 text-xs text-muted-foreground"><tr><th className="p-3 text-start">{t("subscriptions.plan")}</th><th className="p-3 text-start">{t("subscriptions.monthlyPrice")}</th><th className="p-3 text-start">{t("subscriptions.yearlyPrice")}</th><th className="p-3 text-start">{t("subscriptions.planForm.buildings")}</th><th className="p-3 text-start">{t("subscriptions.planForm.users")}</th><th className="p-3 text-start">{t("common.status")}</th><th className="p-3 text-end">{t("subscriptions.actions")}</th></tr></thead><tbody>{plans.data.items.map((plan) => <tr key={plan.id} onClick={() => setSelectedId(plan.id)} className="cursor-pointer border-t border-border hover:bg-muted/40"><td className="p-3"><p className="font-medium">{language === "ar" ? plan.nameAr : plan.nameEn}</p><p className="text-xs text-muted-foreground">{t(plan.pricingModel === "PayAsYouGo" ? "subscriptions.paygPlanForm.paygPricing" : "subscriptions.paygPlanForm.fixedPricing")}</p></td><td className="p-3 whitespace-nowrap">{formatCurrency(plan.pricingModel === "PayAsYouGo" ? plan.paygMonthlyUnitPrice ?? 0 : plan.monthlyPrice, { currency: plan.currency })}</td><td className="p-3 whitespace-nowrap">{formatCurrency(plan.pricingModel === "PayAsYouGo" ? plan.paygYearlyMonthlyEquivalentUnitPrice ?? 0 : plan.yearlyPrice, { currency: plan.currency })}</td><td className="p-3">{plan.maxBuildings == null ? t("subscriptions.planForm.unlimited") : formatNumber(plan.maxBuildings)}</td><td className="p-3">{plan.maxUsers == null ? t("subscriptions.planForm.unlimited") : formatNumber(plan.maxUsers)}</td><td className="p-3"><SubscriptionStatusBadge value={plan.isActive ? "Active" : "Inactive"} /></td><td className="p-3 text-end" onClick={(event) => event.stopPropagation()}>{canLifecycle && <Button variant="ghost" size="sm" onClick={() => setLifecycle(plan)}>{plan.isActive ? t("subscriptions.planForm.deactivate") : t("subscriptions.planForm.activate")}</Button>}</td></tr>)}</tbody></table></div>}
    {totalPages > 1 && <div className="flex justify-end gap-2"><Button size="sm" variant="outline" disabled={page === 1} onClick={() => setPage(page - 1)}>{t("common.back")}</Button><Button size="sm" variant="outline" disabled={page === totalPages} onClick={() => setPage(page + 1)}>{t("common.next")}</Button></div>}
    <DetailsDrawer open={Boolean(selectedId)} title={t("subscriptions.planDetails")} onClose={() => setSelectedId(null)} footer={selected.data && canLifecycle ? <Button variant={selected.data.isActive ? "destructive" : "default"} onClick={() => setLifecycle(selected.data)}>{selected.data.isActive ? t("subscriptions.planForm.deactivate") : t("subscriptions.planForm.activate")}</Button> : undefined}>{selected.isLoading && <Skeleton className="h-56 w-full" />}{selected.isError && <ErrorState title={planError(selected.error, t)} onRetry={() => selected.refetch()} />}{selected.data && <PlanDetails plan={selected.data} language={language} t={t} formatCurrency={formatCurrency} formatNumber={formatNumber} />}</DetailsDrawer>
    <CreatePlanDialog open={formOpen} pending={create.isPending} draft={draft} errors={errors} setField={setField} onClose={() => setFormOpen(false)} onSubmit={createPlan} t={t} />
    <Dialog open={Boolean(lifecycle)} onOpenChange={(open) => !open && !setPlanActive.isPending && setLifecycle(null)}><DialogContent className="sm:max-w-md"><DialogHeader><DialogTitle>{lifecycle?.isActive ? t("subscriptions.planForm.deactivatePlan") : t("subscriptions.planForm.activatePlan")}</DialogTitle></DialogHeader><p className="text-sm text-muted-foreground">{lifecycle?.isActive ? t("subscriptions.deactivateConfirm") : t("subscriptions.activateConfirm")}</p><DialogFooter><Button variant="outline" disabled={setPlanActive.isPending} onClick={() => setLifecycle(null)}>{t("common.cancel")}</Button><Button variant={lifecycle?.isActive ? "destructive" : "default"} disabled={setPlanActive.isPending} onClick={updateLifecycle}>{t("common.confirm")}</Button></DialogFooter></DialogContent></Dialog>
  </section>;
}

function CreatePlanDialog({ open, pending, draft, errors, setField, onClose, onSubmit, t }: { open: boolean; pending: boolean; draft: PlanDraft; errors: Partial<Record<DraftField, string>>; setField: <K extends keyof PlanDraft>(field: K, value: PlanDraft[K]) => void; onClose: () => void; onSubmit: () => void; t: (key: string) => string }) {
  return (
    <Dialog open={open} onOpenChange={(value) => !pending && !value && onClose()}>
      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader><DialogTitle>{t("subscriptions.createPlan")}</DialogTitle></DialogHeader>
        <div className="space-y-6">
          <FormSection title={t("subscriptions.planForm.information")}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={t("subscriptions.nameAr")} error={errors.nameAr}><Input value={draft.nameAr} onChange={(e) => setField("nameAr", e.target.value)} dir="rtl" /></Field>
              <Field label={t("subscriptions.nameEn")} error={errors.nameEn}><Input value={draft.nameEn} onChange={(e) => setField("nameEn", e.target.value)} dir="ltr" /></Field>
              <Field label={t("subscriptions.descriptionAr")} optional={t("subscriptions.optional")}><Textarea value={draft.descriptionAr} onChange={(e) => setField("descriptionAr", e.target.value)} dir="rtl" rows={3} /></Field>
              <Field label={t("subscriptions.descriptionEn")} optional={t("subscriptions.optional")}><Textarea value={draft.descriptionEn} onChange={(e) => setField("descriptionEn", e.target.value)} dir="ltr" rows={3} /></Field>
            </div>
          </FormSection>
          <FormSection title={t("subscriptions.planForm.pricing")}>
            <Field label={t("subscriptions.paygPlanForm.pricingModel")}>
              <Select value={draft.pricingModel} onValueChange={(value) => setField("pricingModel", value as SubscriptionPricingModel)}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Fixed">{t("subscriptions.paygPlanForm.fixedPricing")}</SelectItem>
                  <SelectItem value="PayAsYouGo">{t("subscriptions.paygPlanForm.paygPricing")}</SelectItem>
                </SelectContent>
              </Select>
            </Field>
            <div className="grid gap-4 sm:grid-cols-2">
              {draft.pricingModel === "Fixed" ? (
                <>
                  <PriceField label={t("subscriptions.monthlyPrice")} value={draft.monthlyPrice} error={errors.monthlyPrice} onChange={(value) => setField("monthlyPrice", value)} t={t} />
                  <PriceField label={t("subscriptions.yearlyPrice")} value={draft.yearlyPrice} error={errors.yearlyPrice} onChange={(value) => setField("yearlyPrice", value)} t={t} />
                </>
              ) : (
                <>
                  <PriceField label={t("subscriptions.paygPlanForm.paygMonthlyRate")} value={draft.paygMonthlyUnitPrice} error={errors.paygMonthlyUnitPrice} onChange={(value) => setField("paygMonthlyUnitPrice", value)} t={t} />
                  <PriceField label={t("subscriptions.paygPlanForm.paygYearlyMonthlyRate")} value={draft.paygYearlyMonthlyEquivalentUnitPrice} error={errors.paygYearlyMonthlyEquivalentUnitPrice} onChange={(value) => setField("paygYearlyMonthlyEquivalentUnitPrice", value)} t={t} />
                </>
              )}
            </div>
          </FormSection>
          <FormSection title={t("subscriptions.planForm.limits")}>
            <div className="grid gap-4 sm:grid-cols-2">
              <LimitField label={t("subscriptions.planForm.buildings")} unlimited={draft.unlimitedBuildings} value={draft.maxBuildings} error={errors.maxBuildings} onUnlimited={(value) => { setField("unlimitedBuildings", value); if (value) setField("maxBuildings", null); }} onValue={(value) => setField("maxBuildings", value)} t={t} />
              <LimitField label={t("subscriptions.planForm.users")} unlimited={draft.unlimitedUsers} value={draft.maxUsers} error={errors.maxUsers} onUnlimited={(value) => { setField("unlimitedUsers", value); if (value) setField("maxUsers", null); }} onValue={(value) => setField("maxUsers", value)} t={t} />
            </div>
          </FormSection>
          <FormSection title={t("subscriptions.planForm.trial")}>
            <div className="flex items-center justify-between gap-4 rounded-md border border-border p-3"><span className="text-sm font-medium">{draft.supportsTrial ? t("subscriptions.planForm.enabled") : t("subscriptions.planForm.disabled")}</span><Switch checked={draft.supportsTrial} onCheckedChange={(checked) => { setField("supportsTrial", checked); if (!checked) setField("trialDurationDays", null); }} /></div>
            {draft.supportsTrial && <Field label={t("subscriptions.planForm.trialDuration")} error={errors.trialDurationDays}><div className="flex items-center gap-2"><Input className="max-w-32" type="number" min="1" value={draft.trialDurationDays ?? ""} onChange={(e) => setField("trialDurationDays", e.target.value ? Number(e.target.value) : null)} /><span className="text-sm text-muted-foreground">{t("subscriptions.planForm.days")}</span></div></Field>}
          </FormSection>
        </div>
        <DialogFooter><Button variant="outline" disabled={pending} onClick={onClose}>{t("common.cancel")}</Button><Button disabled={pending} onClick={onSubmit}>{pending ? t("common.saving") : t("subscriptions.createPlan")}</Button></DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
function PriceField({ label, value, error, onChange, t }: { label: string; value: number; error?: string; onChange: (value: number) => void; t: (key: string) => string }) {
  return <Field label={label} error={error}><div className="flex items-center"><Input type="number" min="0.01" step="0.01" value={value || ""} onChange={(e) => onChange(Number(e.target.value))} className="rounded-e-none" /><div className="flex h-10 items-center justify-center rounded-e-md border border-s-0 border-input bg-muted px-3 text-sm text-muted-foreground">{t("subscriptions.planForm.jod")}</div></div></Field>;
}
function FormSection({ title, children }: { title: string; children: React.ReactNode }) { return <section className="space-y-4"><h3 className="text-base font-semibold tracking-tight">{title}</h3>{children}</section>; }
function Field({ label, optional, error, children }: { label: string; optional?: string; error?: string; children: React.ReactNode }) { return <label className="grid gap-2 text-sm"><span className="font-medium">{label}{optional && <span className="ms-1 font-normal text-muted-foreground">({optional})</span>}</span>{children}{error && <span role="alert" className="text-xs text-destructive">{error}</span>}</label>; }
function LimitField({ label, unlimited, value, error, onUnlimited, onValue, t }: { label: string; unlimited: boolean; value?: number | null; error?: string; onUnlimited: (value: boolean) => void; onValue: (value: number | null) => void; t: (key: string) => string }) { return <Field label={label} error={error}><div className="flex items-center gap-4">{!unlimited && <Input type="number" min="1" value={value ?? ""} onChange={(e) => onValue(e.target.value ? Number(e.target.value) : null)} className="max-w-[120px]" />}<span className="flex items-center gap-2 text-sm font-medium"><Switch checked={unlimited} onCheckedChange={onUnlimited} />{t("subscriptions.planForm.unlimited")}</span></div></Field>; }
function PlanDetails({ plan, language, t, formatCurrency, formatNumber }: { plan: SubscriptionPlanDto; language: string; t: (key: string) => string; formatCurrency: (value: number, options: { currency: string }) => string; formatNumber: (value: number) => string }) { return <div className="space-y-6"><div><h3 className="text-lg font-semibold">{language === "ar" ? plan.nameAr : plan.nameEn}</h3><p className="mt-1 text-sm text-muted-foreground">{language === "ar" ? plan.descriptionAr : plan.descriptionEn}</p></div><DetailList items={[{ label: t("subscriptions.paygPlanForm.pricingModel"), value: t(plan.pricingModel === "PayAsYouGo" ? "subscriptions.paygPlanForm.paygPricing" : "subscriptions.paygPlanForm.fixedPricing") }, { label: t("common.status"), value: <SubscriptionStatusBadge value={plan.isActive ? "Active" : "Inactive"} /> }, { label: t(plan.pricingModel === "PayAsYouGo" ? "subscriptions.paygPlanForm.paygMonthlyRate" : "subscriptions.monthlyPrice"), value: formatCurrency(plan.pricingModel === "PayAsYouGo" ? plan.paygMonthlyUnitPrice ?? 0 : plan.monthlyPrice, { currency: plan.currency }) }, { label: t(plan.pricingModel === "PayAsYouGo" ? "subscriptions.paygPlanForm.paygYearlyMonthlyRate" : "subscriptions.yearlyPrice"), value: formatCurrency(plan.pricingModel === "PayAsYouGo" ? plan.paygYearlyMonthlyEquivalentUnitPrice ?? 0 : plan.yearlyPrice, { currency: plan.currency }) }, { label: t("subscriptions.planForm.buildings"), value: plan.maxBuildings == null ? t("subscriptions.planForm.unlimited") : formatNumber(plan.maxBuildings) }, { label: t("subscriptions.planForm.users"), value: plan.maxUsers == null ? t("subscriptions.planForm.unlimited") : formatNumber(plan.maxUsers) }, { label: t("subscriptions.planForm.trial"), value: plan.supportsTrial ? `${plan.trialDurationDays} ${t("subscriptions.planForm.days")}` : t("subscriptions.planForm.disabled") }]} /></div>; }
function planError(error: unknown, t: (key: string) => string) { if (error instanceof ApiError) { const byCode: Record<string, string> = { PLAN_CODE_ALREADY_EXISTS: "subscriptions.planForm.codeExists", CONCURRENCY_CONFLICT: "subscriptions.errors.concurrency" }; if (error.code && byCode[error.code]) return t(byCode[error.code]); } return extractUserFriendlyError(error, t("subscriptions.errors.unexpected")); }

