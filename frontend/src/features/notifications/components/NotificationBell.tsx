import React, { useState, useRef, useEffect } from "react";
import { Bell, Check, CheckCheck, AlertCircle, RefreshCw, ChevronDown, ChevronUp, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "@/shared/i18n";
import {
  useUnreadNotificationCount,
  useMyNotifications,
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
} from "@/features/notifications/hooks/useNotifications";
import type { NotificationDto } from "@/features/notifications/types/notifications.types";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import { canMarkRead } from '../utils/inbox';

export function NotificationBell() {
  const { t, formatDate } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const { data: unreadCount = 0 } = useUnreadNotificationCount({ poll: true });
  const { data: notifications = [], isLoading, isError, error, refetch } = useMyNotifications({ pageSize: 20 });
  const markAsRead = useMarkNotificationAsRead();
  const markAllAsRead = useMarkAllNotificationsAsRead();

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
    // Toggle expand/collapse
    setExpandedId((prev) => (prev === notification.id ? null : notification.id));

    // Read state is separate from delivery status. Only Sent is eligible.
    if (canMarkRead(notification) && !markAsRead.isPending) {
      markAsRead.mutate(notification.id, { onError: error => toast.error(extractUserFriendlyError(error, t('tenant.notifications.actionFailed'))) });
    }
  };

  const handleMarkAllAsRead = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (markAllAsRead.isPending || unreadCount === 0) return;

    markAllAsRead.mutate(undefined, {
      onSuccess: (result) => {
        toast.success(t('mvp.marked', { count: result.markedCount }));
      },
      onError: (error) => {
        toast.error(extractUserFriendlyError(error, t("tenant.notifications.actionFailed")));
      },
    });
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
            aria-label={`${unreadCount} ${t("tenant.notifications.unreadBadge", { count: unreadCount })}`}
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
          <div className="flex items-center justify-between pb-2.5 mb-2 border-b border-border">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-foreground flex items-center gap-1.5 text-xs sm:text-sm">
                <Bell className="w-4 h-4 text-primary" />
                {t("common.notifications")}
              </span>
              {unreadCount > 0 && (
                <span className="text-[10px] bg-primary/10 text-primary font-medium px-2 py-0.5 rounded-full">
                  {t("tenant.notifications.unreadBadge", { count: unreadCount })}
                </span>
              )}
            </div>

            {/* Mark All as Read Action (Single Backend Operation) */}
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={handleMarkAllAsRead}
                disabled={markAllAsRead.isPending}
                aria-label={t("tenant.notifications.markAllAsRead")}
                className="text-[11px] font-medium text-primary hover:text-primary/80 flex items-center gap-1 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {markAllAsRead.isPending ? (
                  <Loader2 className="w-3 h-3 animate-spin" />
                ) : (
                  <CheckCheck className="w-3.5 h-3.5" />
                )}
                <span>{t("tenant.notifications.markAllAsRead")}</span>
              </button>
            )}
          </div>

          {/* List Content */}
          <div className="max-h-96 overflow-y-auto space-y-1.5 pe-1">
            {isLoading ? (
              <div className="space-y-2 py-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="h-12 bg-secondary/60 animate-pulse rounded-lg" />
                ))}
              </div>
            ) : isError ? (
              <div className="py-6 text-center text-muted-foreground space-y-2">
                <AlertCircle className="w-6 h-6 text-destructive mx-auto" />
                <p className="text-xs">{extractUserFriendlyError(error)}</p>
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
                  {t("tenant.notifications.emptyState")}
                </p>
              </div>
            ) : (
              notifications.map((n) => {
                const isUnread = canMarkRead(n);
                const statusLabel = t(`mvp.${n.readAt ? 'read' : ['pending', 'sent', 'failed', 'cancelled'][n.status] ?? 'pending'}`);
                const isExpanded = expandedId === n.id;

                return (
                  <div
                    key={n.id}
                    role="button"
                    tabIndex={0}
                    aria-expanded={isExpanded}
                    aria-label={`${n.subject} - ${statusLabel}`}
                    onClick={() => handleNotificationClick(n)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        handleNotificationClick(n);
                      }
                    }}
                    className={`p-2.5 rounded-lg border transition-all cursor-pointer select-none ${
                      isUnread
                        ? "bg-primary/5 border-primary/20 hover:bg-primary/10"
                        : "bg-secondary/30 border-border/50 hover:bg-secondary/60 opacity-85"
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
                        <div className="flex items-center justify-between gap-1.5">
                          <p className={`text-xs ${isUnread ? "font-semibold text-foreground" : "font-medium text-muted-foreground"}`}>
                            {n.subject}
                          </p>
                          <div className="flex items-center gap-1 shrink-0">
                            <span className="text-[10px] text-muted-foreground font-mono">
                              {formatDate(n.createdAt, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" })}
                            </span>
                            {isExpanded ? (
                              <ChevronUp className="w-3.5 h-3.5 text-muted-foreground" aria-hidden="true" />
                            ) : (
                              <ChevronDown className="w-3.5 h-3.5 text-muted-foreground" aria-hidden="true" />
                            )}
                          </div>
                        </div>

                        {/* Content: Compact vs Expanded */}
                        {isExpanded ? (
                          <div className="mt-2 space-y-2 text-xs text-foreground/90 leading-relaxed break-words whitespace-pre-wrap animate-in fade-in-50 duration-150">
                            <p className="p-2 rounded bg-background/60 border border-border/40 text-[11.5px] leading-relaxed">
                              {n.body}
                            </p>
                            <div className="flex items-center justify-between text-[10px] text-muted-foreground pt-1 border-t border-border/40">
                              <span className="flex items-center gap-1 font-medium text-primary">
                                <Check className="w-3 h-3" aria-hidden="true" />
                                {statusLabel}
                              </span>
                              <span className="font-mono">
                                {formatDate(n.createdAt, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" })}
                              </span>
                            </div>
                          </div>
                        ) : (
                          <p className="text-[11.5px] text-muted-foreground line-clamp-2 mt-0.5 leading-snug">
                            {n.body}
                          </p>
                        )}
                      </div>
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
