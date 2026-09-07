import React, { useState } from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTenantProfile } from "../hooks/useTenantProfile";
import { useUtilityDashboardSummary } from "../hooks/useUtilityBills";
import {
  useUnreadNotificationCount,
  useMyNotifications,
  useMarkNotificationAsRead,
} from "@/features/notifications/hooks/useNotifications";
import { TenantLinkUtilityAccountModal } from "../components/TenantLinkUtilityAccountModal";
import { TenantPaymentBadge, TenantSyncBadge } from "../components/TenantUtilityBadges";
import type { TenantUtilityBillDto } from "../types/utilityBills.types";
import type { TenantUtilitySyncStatus } from "../types/utilityBills.types";
import { utilityBillsErrorMessage } from "../utils/utilityBills";
import { Button } from "@/app/components/ui/button";
import { ErrorState, Skeleton } from "@/shared/components/ui/Feedback";
import {
  UserCircle,
  Bell,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  ChevronUp,
  Check,
  Users,
  Car,
  ShieldCheck,
  ArrowUpRight,
  CreditCard,
  Zap,
  Droplets,
} from "lucide-react";


export function TenantDashboardPage() {
  const { t, language, formatCurrency } = useTranslation();
  const { user } = useAuth();
  const navigate = useNavigate();

  const isRtl = language === "ar";
  const ChevronIcon = isRtl ? ChevronLeft : ChevronRight;

  const { data: profile } = useTenantProfile();
  const {
    data: utilitySummary,
    isLoading: isUtilitySummaryLoading,
    isError: isUtilitySummaryError,
    error: utilitySummaryError,
    refetch: refetchUtilitySummary,
  } = useUtilityDashboardSummary();

  const { data: unreadCount = 0 } = useUnreadNotificationCount();
  const { data: recentNotifications = [] } = useMyNotifications({ pageSize: 5 });
  const markAsRead = useMarkNotificationAsRead();
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [utilityLinkOpen, setUtilityLinkOpen] = useState(false);


  const handleNotificationClick = (n: { id: string; status: number; readAt?: string | null }) => {
    setExpandedId((prev) => (prev === n.id ? null : n.id));
    if (n.status === 1 && !n.readAt && !markAsRead.isPending) {
      markAsRead.mutate(n.id);
    }
  };

  return (
    <PageContainer
      title={t("tenant.dashboard.title")}
      description={t("tenant.dashboard.subtitle")}
    >
      <div className="max-w-4xl space-y-4">
        {/* Welcome Hero Card */}
        <div className="p-6 rounded-2xl bg-card border border-border shadow-xs flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="space-y-1">
            <div className="flex items-center gap-2 text-brand-green-600 dark:text-brand-green-400 font-semibold text-xs uppercase tracking-wider">
              <ShieldCheck className="w-4 h-4" />
              <span>{t("tenant.dashboard.verifiedRole")}</span>
            </div>
            <h2 className="text-xl font-bold text-foreground">
              {isRtl
                ? `مرحباً بك، ${profile?.name || user?.name || ""}`
                : `Welcome back, ${profile?.name || user?.name || ""}`}
            </h2>
            <p className="text-xs text-muted-foreground max-w-xl">
              {t("tenant.dashboard.welcomeDesc")}
            </p>
          </div>

          <button
            onClick={() => navigate("/tenant/profile")}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-primary text-primary-foreground font-medium text-xs shadow-xs hover:bg-primary/90 transition-all cursor-pointer shrink-0"
          >
            <UserCircle className="w-4 h-4" />
            <span>{t("tenant.dashboard.myProfileCard")}</span>
            <ChevronIcon className="w-4 h-4" />
          </button>
        </div>

        {/* Available Features Quick Navigation */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {/* Profile Card */}
          <div
            onClick={() => navigate("/tenant/profile")}
            className="p-5 rounded-xl border border-border bg-card hover:bg-secondary/40 transition-all cursor-pointer group shadow-2xs flex flex-col justify-between space-y-3"
          >
            <div className="flex items-start justify-between">
              <div className="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center font-bold">
                <UserCircle className="w-5 h-5" />
              </div>
              <ArrowUpRight className="w-4 h-4 text-muted-foreground group-hover:text-primary transition-colors" />
            </div>

            <div>
              <h3 className="font-semibold text-sm text-foreground group-hover:text-primary transition-colors">
                {t("tenant.dashboard.myProfileCard")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {t("tenant.dashboard.myProfileDesc")}
              </p>
            </div>

            {profile && (
              <div className="flex items-center gap-3 pt-2 border-t border-border/60 text-[11px] text-muted-foreground">
                <span className="flex items-center gap-1">
                  <Users className="w-3.5 h-3.5 text-primary" />
                  {profile.familyMembers.length} {isRtl ? "عائلة" : "Family"}
                </span>
                <span className="flex items-center gap-1">
                  <Car className="w-3.5 h-3.5 text-primary" />
                  {profile.vehicles.length} {isRtl ? "مركبة" : "Vehicles"}
                </span>
              </div>
            )}
          </div>

          {/* Submit Payment Proof Card */}
          <div
            onClick={() => navigate("/tenant/payments")}
            className="p-5 rounded-xl border border-border bg-card hover:bg-secondary/40 transition-all cursor-pointer group shadow-2xs flex flex-col justify-between space-y-3"
          >
            <div className="flex items-start justify-between">
              <div className="w-10 h-10 rounded-xl bg-brand-green-900/10 text-brand-green-600 dark:text-brand-green-400 flex items-center justify-center font-bold">
                <CreditCard className="w-5 h-5" />
              </div>
              <ArrowUpRight className="w-4 h-4 text-muted-foreground group-hover:text-primary transition-colors" />
            </div>

            <div>
              <h3 className="font-semibold text-sm text-foreground group-hover:text-primary transition-colors">
                {t("paymentVerification.modalTitle")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {t("paymentVerification.modalSubtitle")}
              </p>
            </div>

            <div className="pt-2 border-t border-border/60 text-[11px] text-muted-foreground">
              <span>{isRtl ? "إرسال إيصال وتحويل" : "Submit receipt & transfer ref"}</span>
            </div>
          </div>

          {/* Notifications Card */}
          <div className="p-5 rounded-xl border border-border bg-card hover:bg-secondary/40 transition-all cursor-pointer group shadow-2xs flex flex-col justify-between space-y-3">
            <div className="flex items-start justify-between">
              <div className="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center font-bold relative">
                <Bell className="w-5 h-5" />
                {unreadCount > 0 && (
                  <span className="absolute -top-1 -end-1 w-3 h-3 rounded-full bg-destructive" />
                )}
              </div>
              <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-secondary text-foreground border border-border">
                {unreadCount} {isRtl ? "جديد" : "Unread"}
              </span>
            </div>

            <div>
              <h3 className="font-semibold text-sm text-foreground group-hover:text-primary transition-colors">
                {t("tenant.dashboard.notificationsCard")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {t("tenant.dashboard.notificationsDesc")}
              </p>
            </div>

            <div className="pt-2 border-t border-border/60 text-[11px] text-muted-foreground">
              <span>{recentNotifications.length} {isRtl ? "إشعارات إجمالية" : "total notifications in inbox"}</span>
            </div>
          </div>
        </div>

        {/* Utility Bills Summary Widget */}
        <div className="rounded-2xl border border-border bg-card shadow-xs p-5 space-y-4">
          <div className="flex items-center justify-between border-b border-border/70 pb-3">
            <div className="flex items-center gap-2">
              <Zap className="w-4 h-4 text-amber-500" />
              <h3 className="font-semibold text-sm">{t("tenant.utilityBills.dashboard.title")}</h3>
            </div>
            <button
              onClick={() => navigate("/tenant/bills")}
              className="text-xs font-semibold text-primary hover:underline flex items-center gap-1 cursor-pointer"
            >
              <span>{t("tenant.utilityBills.dashboard.viewDetails")}</span>
              <ChevronIcon className="w-3.5 h-3.5" />
            </button>
          </div>

          {isUtilitySummaryLoading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Skeleton className="h-24 w-full rounded-xl" />
              <Skeleton className="h-24 w-full rounded-xl" />
            </div>
          ) : isUtilitySummaryError ? (
            <ErrorState
              message={utilityBillsErrorMessage(utilitySummaryError, t)}
              onRetry={() => refetchUtilitySummary()}
            />
          ) : !utilitySummary?.electricityLinked && !utilitySummary?.waterLinked ? (
            <div className="py-6 px-4 text-center text-muted-foreground text-xs bg-secondary/30 rounded-xl space-y-3">
              <div className="space-y-1">
                <p className="font-medium text-foreground">{t("tenant.utilityBills.dashboard.noAccountsTitle")}</p>
                <p className="text-[11px]">{t("tenant.utilityBills.dashboard.noAccountsDescription")}</p>
              </div>
              <Button size="sm" onClick={() => setUtilityLinkOpen(true)}>
                {t("tenant.utilityBills.dashboard.linkAction")}
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <DashboardUtilitySummary
                icon={Zap}
                iconClass="text-amber-500"
                title={t("tenant.utilityBills.electricity")}
                linked={Boolean(utilitySummary?.electricityLinked)}
                latestBill={utilitySummary?.latestElectricityBill}
                accountNumber={utilitySummary?.electricityAccountNumber}
                syncStatus={utilitySummary?.electricitySyncStatus}
                lastSuccessfulSyncAt={utilitySummary?.electricityLastSuccessfulSyncAt}
                onClick={() => navigate("/tenant/bills?utilityType=Electricity")}
                formatAmount={(amount, currency) => formatCurrency(amount, { currency })}
              />
              <DashboardUtilitySummary
                icon={Droplets}
                iconClass="text-sky-500"
                title={t("tenant.utilityBills.water")}
                linked={Boolean(utilitySummary?.waterLinked)}
                latestBill={utilitySummary?.latestWaterBill}
                accountNumber={utilitySummary?.waterAccountNumber}
                syncStatus={utilitySummary?.waterSyncStatus}
                lastSuccessfulSyncAt={utilitySummary?.waterLastSuccessfulSyncAt}
                onClick={() => navigate("/tenant/bills?utilityType=Water")}
                formatAmount={(amount, currency) => formatCurrency(amount, { currency })}
              />
            </div>
          )}
        </div>

        {/* Recent Notifications Summary Widget */}
        <div className="rounded-xl border border-border bg-card shadow-xs p-5 space-y-4">

          <div className="flex items-center justify-between border-b border-border/70 pb-3">
            <div className="flex items-center gap-2">
              <Bell className="w-4 h-4 text-primary" />
              <h3 className="font-semibold text-sm">
                {t("tenant.dashboard.recentNotificationsTitle")}
              </h3>
            </div>
          </div>

          {recentNotifications.length === 0 ? (
            <div className="py-8 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg space-y-1">
              <Bell className="w-6 h-6 mx-auto opacity-30" />
              <p>{t("tenant.dashboard.noNotificationsYet")}</p>
            </div>
          ) : (
            <div className="space-y-2">
              {recentNotifications.map((n) => {
                const isUnread = n.status === 1 && !n.readAt;
                const isExpanded = expandedId === n.id;

                return (
                  <div
                    key={n.id}
                    role="button"
                    tabIndex={0}
                    aria-expanded={isExpanded}
                    aria-label={`${n.subject} - ${isUnread ? t("tenant.notifications.unreadStatus") : t("tenant.notifications.readStatus")}`}
                    onClick={() => handleNotificationClick(n)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        handleNotificationClick(n);
                      }
                    }}
                    className={`p-3 rounded-lg border transition-all cursor-pointer select-none text-xs ${
                      isUnread
                        ? "border-primary/25 bg-primary/5 hover:bg-primary/10 shadow-2xs"
                        : "border-border/60 bg-secondary/20 hover:bg-secondary/40 opacity-90"
                    }`}
                  >
                    <div className="flex items-start gap-2.5">
                      <div
                        className={`w-2 h-2 rounded-full mt-1.5 shrink-0 ${
                          isUnread ? "bg-primary" : "bg-transparent"
                        }`}
                        aria-hidden="true"
                      />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between gap-2">
                          <p className={`font-semibold ${isUnread ? "text-foreground" : "text-muted-foreground"}`}>
                            {n.subject}
                          </p>
                          <div className="flex items-center gap-1 shrink-0">
                            <span className="text-[10px] text-muted-foreground font-mono">
                              {new Date(n.createdAt).toLocaleDateString(isRtl ? "ar-JO" : "en-US", {
                                month: "short",
                                day: "numeric",
                                hour: "2-digit",
                                minute: "2-digit",
                              })}
                            </span>
                            {isExpanded ? (
                              <ChevronUp className="w-3.5 h-3.5 text-muted-foreground" aria-hidden="true" />
                            ) : (
                              <ChevronDown className="w-3.5 h-3.5 text-muted-foreground" aria-hidden="true" />
                            )}
                          </div>
                        </div>

                        {/* Message body */}
                        {isExpanded ? (
                          <div className="mt-2 space-y-2 text-foreground/90 leading-relaxed break-words whitespace-pre-wrap animate-in fade-in-50 duration-150">
                            <p className="p-2.5 rounded bg-background/60 border border-border/40 text-[11.5px] leading-relaxed">
                              {n.body}
                            </p>
                            <div className="flex items-center justify-between text-[10px] text-muted-foreground pt-1 border-t border-border/40">
                              <span className="flex items-center gap-1 font-medium text-primary">
                                <Check className="w-3 h-3" aria-hidden="true" />
                                {t("tenant.notifications.readStatus")}
                              </span>
                              <span className="font-mono">
                                {new Date(n.createdAt).toLocaleDateString(isRtl ? "ar-JO" : "en-US")}
                              </span>
                            </div>
                          </div>
                        ) : (
                          <p className="text-muted-foreground text-[11.5px] line-clamp-1 mt-0.5 leading-snug">
                            {n.body}
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
      <TenantLinkUtilityAccountModal
        open={utilityLinkOpen}
        onClose={() => setUtilityLinkOpen(false)}
      />
    </PageContainer>
  );
}

interface DashboardUtilitySummaryProps {
  icon: typeof Zap;
  iconClass: string;
  title: string;
  linked: boolean;
  latestBill?: TenantUtilityBillDto | null;
  accountNumber?: string | null;
  syncStatus?: TenantUtilitySyncStatus | null;
  lastSuccessfulSyncAt?: string | null;
  onClick: () => void;
  formatAmount: (amount: number, currency: string) => string;
}

function DashboardUtilitySummary({
  icon: Icon,
  iconClass,
  title,
  linked,
  latestBill,
  accountNumber,
  syncStatus,
  lastSuccessfulSyncAt,
  onClick,
  formatAmount,
}: DashboardUtilitySummaryProps) {
  const { t } = useTranslation();

  return (
    <button type="button" onClick={onClick} className="w-full text-start p-4 rounded-xl border border-border/70 bg-secondary/20 hover:bg-secondary/40 transition-colors space-y-2">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Icon className={`w-4 h-4 ${iconClass}`} />
          <span className="font-semibold text-xs text-foreground">{title}</span>
        </div>
        {linked && syncStatus != null ? <TenantSyncBadge value={syncStatus} /> : <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold border bg-muted text-muted-foreground border-border">{t("tenant.utilityBills.states.notLinked")}</span>}
      </div>
      {linked && <div className="grid grid-cols-2 gap-2 text-[10px] text-muted-foreground"><span className="font-mono truncate">{accountNumber ?? "—"}</span><span className="text-end">{lastSuccessfulSyncAt ? new Date(lastSuccessfulSyncAt).toLocaleDateString() : t("tenant.utilityBills.states.neverSynced")}</span></div>}
      {latestBill ? (
        <div className="pt-2 border-t border-border/40 flex items-center justify-between gap-2 text-xs">
          <div>
            <p className="text-[11px] text-muted-foreground">{t("tenant.utilityBills.latestBill")}</p>
            <p className="font-bold text-foreground mt-0.5">
              {formatAmount(latestBill.amount, latestBill.currency)}
            </p>
          </div>
          <TenantPaymentBadge value={latestBill.paymentStatus} />
        </div>
      ) : linked ? (
        <p className="text-[11px] text-muted-foreground pt-1">{t("tenant.utilityBills.states.noBills")}</p>
      ) : null}
    </button>
  );
}
