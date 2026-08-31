import { CalendarDays, CircleDollarSign, Gauge, Users } from "lucide-react";
import { useCurrentSubscriptionUsage } from "@/features/subscriptions/hooks/useSubscriptions";
import { useTranslation } from "@/shared/i18n";
import { Skeleton } from "@/shared/ui/skeleton";

export function PaygUsageSection() {
  const { data, isLoading, isError } = useCurrentSubscriptionUsage();
  const { t, language, formatCurrency, formatDate, formatNumber } = useTranslation();

  if (isLoading) return <Skeleton className="h-64 w-full rounded-lg" />;
  if (isError || !data) return null;
  if (!data.isPayAsYouGo) return null;

  const planName = language === "ar" ? data.planNameAr : data.planNameEn;
  const money = (value: number) => formatCurrency(value, { currency: data.currency });
  const periodEnd = new Date(`${data.billingPeriodEnd}T00:00:00Z`);
  periodEnd.setUTCDate(periodEnd.getUTCDate() - 1);
  const inclusivePeriodEnd = periodEnd.toISOString().slice(0, 10);

  return (
    <section className="rounded-lg border border-outline-variant bg-card p-5 shadow-e0" aria-labelledby="payg-usage-title">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="type-label-medium font-semibold uppercase tracking-wide text-primary">{t("dashboard.payg.label")}</p>
          <h2 id="payg-usage-title" className="type-title-large mt-1 text-foreground">
            {planName} · {t(`dashboard.payg.cycle.${data.billingCycle}`)}
          </h2>
          <p className="mt-1 type-body-small text-muted-foreground">
            {data.isEstimated ? t("dashboard.payg.estimatedNotice") : t("dashboard.payg.finalizedNotice")}
          </p>
        </div>
        <div className="rounded-md bg-primary/10 px-3 py-2 text-sm font-semibold text-primary">
          {money(data.monthlyEquivalentUnitPrice)} / {t("dashboard.payg.activeLeaseUnit")}
        </div>
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Metric icon={Users} label={t("dashboard.payg.activeLeases")} value={formatNumber(data.currentActiveLeaseCount)} />
        <Metric icon={Gauge} label={t("dashboard.payg.accumulatedUsage")} value={`${formatNumber(data.accumulatedLeaseDays)} ${t("dashboard.payg.leaseDays")}`} />
        <Metric icon={CircleDollarSign} label={t("dashboard.payg.estimatedSoFar")} value={money(data.estimatedAmount)} />
        <Metric icon={CircleDollarSign} label={t("dashboard.payg.projectedAmount")} value={money(data.projectedPeriodAmount)} />
      </div>

      <div className="mt-4 grid gap-3 border-t border-outline-variant pt-4 text-sm sm:grid-cols-3">
        <Detail icon={CalendarDays} label={t("dashboard.payg.billingPeriod")} value={`${formatDate(data.billingPeriodStart, { dateStyle: "medium" })} → ${formatDate(inclusivePeriodEnd, { dateStyle: "medium" })}`} />
        <Detail label={t("dashboard.payg.daysElapsed")} value={formatNumber(data.daysElapsed)} />
        <Detail label={t("dashboard.payg.daysRemaining")} value={formatNumber(data.daysRemaining)} />
      </div>
    </section>
  );
}

function Metric({ icon: Icon, label, value }: { icon: typeof Users; label: string; value: string }) {
  return <div className="rounded-md border border-outline-variant bg-surface-container-low p-4"><div className="flex items-center gap-2 text-muted-foreground"><Icon className="h-4 w-4" /><span className="type-label-medium">{label}</span></div><p className="mt-2 type-title-large font-semibold text-foreground">{value}</p></div>;
}

function Detail({ icon: Icon, label, value }: { icon?: typeof CalendarDays; label: string; value: string }) {
  return <div className="flex items-start gap-2">{Icon && <Icon className="mt-0.5 h-4 w-4 text-muted-foreground" />}<div><p className="text-muted-foreground">{label}</p><p className="font-medium text-foreground">{value}</p></div></div>;
}
