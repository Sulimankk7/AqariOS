import React from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "@/shared/i18n";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useQuery } from "@tanstack/react-query";
import { tenantPortalApi } from "../api/tenantPortal.api";
import {
  useUnreadNotificationCount,
  useMyNotifications,
} from "@/features/notifications/hooks/useNotifications";
import {
  UserCircle,
  Bell,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Users,
  Car,
  ShieldCheck,
  ArrowUpRight,
} from "lucide-react";

export function TenantDashboardPage() {
  const { t, language } = useTranslation();
  const { user } = useAuth();
  const navigate = useNavigate();
  const userId = user?.id;

  const isRtl = language === "ar";
  const ChevronIcon = isRtl ? ChevronLeft : ChevronRight;

  const { data: profile } = useQuery({
    queryKey: ["tenant", userId, "profile"],
    queryFn: () => tenantPortalApi.getProfile(),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
  });

  const { data: unreadCount = 0 } = useUnreadNotificationCount();
  const { data: recentNotifications = [] } = useMyNotifications({ pageSize: 5 });

  return (
    <PageContainer
      title={t("tenant.dashboard.title", "Dashboard")}
      description={t("tenant.dashboard.subtitle", "Overview of your account and services")}
    >
      <div className="space-y-6">
        {/* Welcome Header Hero Card */}
        <div className="rounded-2xl border border-primary/20 bg-gradient-to-r from-primary/10 via-primary/5 to-transparent p-6 sm:p-8 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-2xs">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-primary/15 text-primary border border-primary/20">
                {t("tenant.dashboard.accountActive", "Account Active & Verified")}
              </span>
            </div>
            <h2 className="text-xl sm:text-2xl font-bold text-foreground tracking-tight pt-1">
              {t("tenant.welcomeUser", "Welcome, {{name}}", { name: user?.name || profile?.name || "Tenant" })}
            </h2>
            <p className="text-xs sm:text-sm text-muted-foreground">
              {t("tenant.welcomeSubtitle", "Tenant Self-Service Portal — View your profile details and notifications.")}
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
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
              {recentNotifications.map((n) => (
                <div
                  key={n.id}
                  className="p-3 rounded-lg border border-border/60 bg-secondary/20 flex items-start justify-between gap-3 text-xs"
                >
                  <div className="space-y-0.5">
                    <p className="font-semibold text-foreground">{n.subject}</p>
                    <p className="text-muted-foreground text-[11.5px] line-clamp-1">{n.body}</p>
                  </div>
                  <span className="text-[10px] text-muted-foreground shrink-0 font-mono">
                    {new Date(n.createdAt).toLocaleDateString(isRtl ? "ar-JO" : "en-US")}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </PageContainer>
  );
}
