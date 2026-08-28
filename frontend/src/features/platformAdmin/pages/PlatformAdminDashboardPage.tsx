import { Link } from "react-router";
import { ArrowLeft, ArrowRight, Building2, ClipboardList, CreditCard, RefreshCw } from "lucide-react";
import { ROUTES } from "@/config/routes";
import { useTranslation } from "@/shared/i18n";
import { Skeleton } from "@/shared/ui/skeleton";
import { usePendingLandlordRegistrations } from "../hooks/useLandlordRegistrations";
import { usePlatformCompanies, usePlatformPlanChangeRequests, usePlatformSubscriptions } from "@/features/subscriptions/hooks/useSubscriptions";

export function PlatformAdminDashboardPage() {
  const { t, formatNumber, direction } = useTranslation();
  const registrations = usePendingLandlordRegistrations(1, 1);
  const companies = usePlatformCompanies();
  const active = usePlatformSubscriptions({ page: 1, pageSize: 1, status: "Active" });
  const trials = usePlatformSubscriptions({ page: 1, pageSize: 1, status: "Trialing" });
  const expired = usePlatformSubscriptions({ page: 1, pageSize: 1, status: "Expired" });
  const planChanges = usePlatformPlanChangeRequests({ page: 1, pageSize: 1, status: "Pending" });
  const arrow = direction === "rtl" ? <ArrowLeft className="h-3.5 w-3.5" /> : <ArrowRight className="h-3.5 w-3.5" />;
  const retryLabel = t("common.retry");

  const metrics = [
    { label: t("platformAdmin.dashboard.activeCompanies"), value: companies.data?.length, loading: companies.isLoading, error: companies.isError, icon: Building2 },
    { label: t("platformAdmin.dashboard.activeSubscriptions"), value: active.data?.totalCount, loading: active.isLoading, error: active.isError, icon: CreditCard },
  ];
  const subscriptionMetrics = [
    { label: t("platformAdmin.dashboard.activeSubscriptions"), query: active },
    { label: t("platformAdmin.dashboard.trialSubscriptions"), query: trials },
    { label: t("platformAdmin.dashboard.expiredSubscriptions"), query: expired },
    { label: t("platformAdmin.dashboard.pendingPlanChanges"), query: planChanges },
  ];

  return <div className="space-y-7">
    <header className="space-y-1"><h1 className="text-xl font-bold tracking-tight text-foreground sm:text-2xl">{t("platformAdmin.dashboard.title")}</h1><p className="text-xs text-muted-foreground sm:text-sm">{t("platformAdmin.dashboard.subtitle")}</p></header>

    <section aria-label={t("platformAdmin.dashboard.overview")} className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      {metrics.map((metric) => <MetricCard key={metric.label} {...metric} formatNumber={formatNumber} />)}
    </section>

    <section className="space-y-3">
      <div><h2 className="text-base font-semibold">{t("platformAdmin.dashboard.needsReview")}</h2><p className="mt-0.5 text-xs text-muted-foreground">{t("platformAdmin.dashboard.needsReviewDescription")}</p></div>
      <div className="grid gap-3 lg:grid-cols-2">
        <AttentionItem title={t("platformAdmin.dashboard.pendingRegistrations")} count={registrations.data?.totalCount} loading={registrations.isLoading} error={registrations.isError} retry={() => registrations.refetch()} href={ROUTES.platform.landlordRegistrations} action={t("platformAdmin.dashboard.reviewRequests")} arrow={arrow} retryLabel={retryLabel} formatNumber={formatNumber} />
        <AttentionItem title={t("platformAdmin.dashboard.pendingPlanChanges")} count={planChanges.data?.totalCount} loading={planChanges.isLoading} error={planChanges.isError} retry={() => planChanges.refetch()} href={ROUTES.platform.planChangeRequests} action={t("platformAdmin.dashboard.reviewPlanChanges")} arrow={arrow} retryLabel={retryLabel} formatNumber={formatNumber} />
      </div>
    </section>

    <section className="space-y-3">
      <div className="flex items-center justify-between gap-3"><h2 className="text-base font-semibold">{t("platformAdmin.dashboard.subscriptionsOverview")}</h2><Link className="text-xs font-medium text-primary hover:underline" to={ROUTES.platform.subscriptions}>{t("platformAdmin.dashboard.viewSubscriptions")}</Link></div>
      <div className="overflow-hidden rounded-lg border border-border bg-card">
        {subscriptionMetrics.map(({ label, query }, index) => <div key={label} className={`flex items-center justify-between gap-4 px-4 py-3 text-sm ${index ? "border-t border-border" : ""}`}><span className="text-muted-foreground">{label}</span>{query.isLoading ? <Skeleton className="h-5 w-12" /> : query.isError ? <button className="text-xs text-destructive hover:underline" onClick={() => query.refetch()}>{retryLabel}</button> : <span className="font-semibold tabular-nums">{formatNumber(query.data?.totalCount ?? 0)}</span>}</div>)}
      </div>
    </section>
  </div>;
}

function MetricCard({ label, value, loading, error, icon: Icon, formatNumber }: { label: string; value?: number; loading: boolean; error: boolean; icon: typeof Building2; formatNumber: (value: number) => string }) {
  return <div className="rounded-lg border border-border bg-card p-4"><div className="flex items-center justify-between gap-3"><p className="text-xs text-muted-foreground">{label}</p><Icon className="h-4 w-4 text-muted-foreground" /></div>{loading ? <Skeleton className="mt-3 h-8 w-20" /> : error ? <p className="mt-3 text-sm text-destructive">—</p> : <p className="mt-2 text-2xl font-bold tabular-nums">{formatNumber(value ?? 0)}</p>}</div>;
}

function AttentionItem({ title, count, loading, error, retry, href, action, arrow, retryLabel, formatNumber }: { title: string; count?: number; loading: boolean; error: boolean; retry: () => unknown; href: string; action: string; arrow: React.ReactNode; retryLabel: string; formatNumber: (value: number) => string }) {
  return <div className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-primary/30 bg-card p-4"><div className="flex items-center gap-3"><span className="rounded-md bg-primary/10 p-2 text-primary"><ClipboardList className="h-4 w-4" /></span><div><p className="text-sm font-medium">{title}</p>{loading ? <Skeleton className="mt-1 h-5 w-16" /> : error ? <button onClick={retry} className="mt-1 inline-flex items-center gap-1 text-xs text-destructive hover:underline"><RefreshCw className="h-3 w-3" />{retryLabel}</button> : <p className="mt-0.5 text-xl font-bold tabular-nums">{formatNumber(count ?? 0)}</p>}</div></div><Link to={href} className="inline-flex h-9 items-center gap-2 rounded-md bg-primary px-3 text-xs font-medium text-primary-foreground hover:bg-primary/90">{action}{arrow}</Link></div>;
}
