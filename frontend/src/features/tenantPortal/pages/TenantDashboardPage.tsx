import React, { useState } from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTenantProfile } from "../hooks/useTenantProfile";
import {
  useUnreadNotificationCount,
  useMyNotifications,
  useMarkNotificationAsRead,
} from "@/features/notifications/hooks/useNotifications";
import { SubmitPaymentVerificationModal } from "../components/SubmitPaymentVerificationModal";
import {
  UserCircle,
  Bell,
  CheckCircle2,
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
} from "lucide-react";

export function TenantDashboardPage() {
  const { t, language } = useTranslation();
  const { user } = useAuth();
  const navigate = useNavigate();

  const isRtl = language === "ar";
  const ChevronIcon = isRtl ? ChevronLeft : ChevronRight;

  const { data: profile } = useTenantProfile();

  const { data: unreadCount = 0 } = useUnreadNotificationCount();
  const { data: recentNotifications = [] } = useMyNotifications({ pageSize: 5 });
  const markAsRead = useMarkNotificationAsRead();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const handleNotificationClick = (n: { id: string; status: number; readAt?: string | null }) => {
    setExpandedId((prev) => (prev === n.id ? null : n.id));
    if (n.status === 0 || !n.readAt) {
      markAsRead.mutate(n.id);
    }
  };

  return (
    <PageContainer
      title={t("tenant.dashboard.title", "Tenant Portal")}
      description={t("tenant.dashboard.subtitle", "Welcome to your AqariOS Tenant Self-Service Portal.")}
    >
      <div className="max-w-4xl space-y-4">
        {/* Welcome Hero Card */}
        <div className="p-6 rounded-2xl bg-card border border-border shadow-xs flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="space-y-1">
            <div className="flex items-center gap-2 text-brand-green-600 dark:text-brand-green-400 font-semibold text-xs uppercase tracking-wider">
              <ShieldCheck className="w-4 h-4" />
              <span>{t("tenant.dashboard.verifiedRole", "Authenticated Tenant Session")}</span>
            </div>
            <h2 className="text-xl font-bold text-foreground">
              {isRtl
                ? `مرحباً بك، ${profile?.name || user?.name || ""}`
                : `Welcome back, ${profile?.name || user?.name || ""}`}
            </h2>
            <p className="text-xs text-muted-foreground max-w-xl">
              {t("tenant.dashboard.welcomeDesc", "Manage your tenant profile, notifications, active lease contract, and payment verification.")}
            </p>
          </div>

          <button
            onClick={() => navigate("/tenant/profile")}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-primary text-primary-foreground font-medium text-xs shadow-xs hover:bg-primary/90 transition-all cursor-pointer shrink-0"
          >
            <UserCircle className="w-4 h-4" />
            <span>{t("tenant.dashboard.myProfileCard", "My Profile")}</span>
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
                {t("tenant.dashboard.myProfileCard", "My Profile")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {t("tenant.dashboard.myProfileDesc", "View your personal information, employment, family members, and vehicles.")}
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
                {t("paymentVerification.modalTitle", "Submit Payment Proof")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {t("paymentVerification.modalSubtitle", "Upload your receipt and submit payment details for property management review.")}
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
                {t("tenant.dashboard.notificationsCard", "Notification Center")}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {t("tenant.dashboard.notificationsDesc", "Track all alerts and messages received from property management.")}
              </p>
            </div>

            <div className="pt-2 border-t border-border/60 text-[11px] text-muted-foreground">
              <span>{recentNotifications.length} {isRtl ? "إشعارات إجمالية" : "total notifications in inbox"}</span>
            </div>
          </div>
        </div>

        {/* Recent Notifications Summary Widget */}
        <div className="rounded-xl border border-border bg-card shadow-xs p-5 space-y-4">
          <div className="flex items-center justify-between border-b border-border/70 pb-3">
            <div className="flex items-center gap-2">
              <Bell className="w-4 h-4 text-primary" />
              <h3 className="font-semibold text-sm">
                {t("tenant.dashboard.recentNotificationsTitle", "Recent Notifications")}
              </h3>
            </div>
          </div>

          {recentNotifications.length === 0 ? (
            <div className="py-8 text-center text-muted-foreground text-xs bg-secondary/30 rounded-lg space-y-1">
              <Bell className="w-6 h-6 mx-auto opacity-30" />
              <p>{t("tenant.dashboard.noNotificationsYet", "No notifications at this time.")}</p>
            </div>
          ) : (
            <div className="space-y-2">
              {recentNotifications.map((n) => {
                const isUnread = n.status === 0 || !n.readAt;
                const isExpanded = expandedId === n.id;

                return (
                  <div
                    key={n.id}
                    role="button"
                    tabIndex={0}
                    aria-expanded={isExpanded}
                    aria-label={`${n.subject} - ${isUnread ? t("tenant.notifications.unreadStatus", "Unread") : t("tenant.notifications.readStatus", "Read")}`}
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
                                {t("tenant.notifications.readStatus", "Read")}
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
    </PageContainer>
  );
}
