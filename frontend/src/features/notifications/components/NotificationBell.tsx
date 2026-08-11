import React, { useState, useRef, useEffect } from "react";
import { Bell, Check, AlertCircle, Info, RefreshCw } from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import {
  useUnreadNotificationCount,
  useMyNotifications,
  useMarkNotificationAsRead,
} from "@/features/notifications/hooks/useNotifications";
import type { NotificationDto } from "@/features/notifications/types/notifications.types";

export function NotificationBell() {
  const { t, language } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const { data: unreadCount = 0 } = useUnreadNotificationCount();
  const { data: notifications = [], isLoading, isError, refetch } = useMyNotifications({ pageSize: 20 });
  const markAsRead = useMarkNotificationAsRead();

  // Close dropdown on click outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleNotificationClick = (notification: NotificationDto) => {
    // If unread (status === 0 or readAt is null), mark as read
    if (notification.status === 0 || !notification.readAt) {
      markAsRead.mutate(notification.id);
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return date.toLocaleDateString(language === "ar" ? "ar-JO" : "en-US", {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        aria-label={t("common.notifications")}
        aria-expanded={isOpen}
        className="w-9 h-9 flex items-center justify-center relative rounded-lg border border-border/60 hover:bg-secondary text-foreground transition-all cursor-pointer"
        title={t("common.notifications")}
      >
        <Bell className="w-4 h-4 text-foreground" />
        {unreadCount > 0 && (
          <span
            className="absolute -top-1 -end-1 min-w-[18px] h-[18px] px-1 rounded-full bg-destructive text-destructive-foreground flex items-center justify-center text-[10px] font-bold ring-2 ring-card animate-pulse"
            aria-label={`${unreadCount} unread notifications`}
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          className="absolute end-0 top-full mt-1.5 w-80 sm:w-96 rounded-xl border border-border bg-card shadow-xl p-3 text-xs z-50 animate-in fade-in-50 zoom-in-95 duration-100"
          role="dialog"
          aria-label={t("common.notifications")}
        >
          {/* Header */}
          <div className="flex items-center justify-between pb-2 mb-2 border-b border-border">
            <span className="font-semibold text-foreground flex items-center gap-1.5 text-xs sm:text-sm">
              <Bell className="w-4 h-4 text-primary" />
              {t("common.notifications")}
            </span>
            {unreadCount > 0 && (
              <span className="text-[10px] bg-primary/10 text-primary font-medium px-2 py-0.5 rounded-full">
                {unreadCount} {language === "ar" ? "جديد" : "New"}
              </span>
            )}
          </div>

          {/* List Content */}
          <div className="max-h-80 overflow-y-auto space-y-1.5 pe-1">
            {isLoading ? (
              <div className="space-y-2 py-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="h-12 bg-secondary/60 animate-pulse rounded-lg" />
                ))}
              </div>
            ) : isError ? (
              <div className="py-6 text-center text-muted-foreground space-y-2">
                <AlertCircle className="w-6 h-6 text-destructive mx-auto" />
                <p className="text-xs">{t("errors.generic")}</p>
                <button
                  onClick={() => refetch()}
                  className="text-primary hover:underline text-[11px] font-medium flex items-center gap-1 mx-auto cursor-pointer"
                >
                  <RefreshCw className="w-3 h-3" />
                  {t("common.retry")}
                </button>
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-8 text-muted-foreground gap-2 text-center">
                <Bell className="w-8 h-8 opacity-25" />
                <p className="font-medium text-foreground">{t("common.noNotifications")}</p>
                <p className="text-[11px] text-muted-foreground">
                  {language === "ar" ? "لا توجد إشعارات في صندوق الوارد حالياً." : "No notifications in your inbox right now."}
                </p>
              </div>
            ) : (
              notifications.map((n) => {
                const isUnread = n.status === 0 || !n.readAt;

                return (
                  <div
                    key={n.id}
                    onClick={() => handleNotificationClick(n)}
                    className={`p-2.5 rounded-lg border transition-colors cursor-pointer flex items-start gap-2.5 ${
                      isUnread
                        ? "bg-primary/5 border-primary/20 hover:bg-primary/10"
                        : "bg-secondary/30 border-border/50 hover:bg-secondary/60 opacity-85"
                    }`}
                  >
                    <div
                      className={`w-2 h-2 rounded-full mt-1.5 shrink-0 ${
                        isUnread ? "bg-primary" : "bg-transparent"
                      }`}
                    />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between gap-1">
                        <p className={`truncate text-xs ${isUnread ? "font-semibold text-foreground" : "font-medium text-muted-foreground"}`}>
                          {n.subject}
                        </p>
                        <span className="text-[10px] text-muted-foreground shrink-0 font-mono">
                          {formatDate(n.createdAt)}
                        </span>
                      </div>
                      <p className="text-[11.5px] text-muted-foreground line-clamp-2 mt-0.5 leading-snug">
                        {n.body}
                      </p>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
}
