/**
 * NotificationItem Component — Accessible notification card item.
 */

import React from "react";
import { Bell, AlertTriangle, CheckCircle, Info } from "lucide-react";

export type NotificationType = "info" | "success" | "warning" | "error";

export interface NotificationItemProps {
  id: string;
  title: string;
  message: string;
  timestamp: string;
  isRead?: boolean;
  type?: NotificationType;
  onClick?: () => void;
}

const typeIconMap = {
  info: Info,
  success: CheckCircle,
  warning: AlertTriangle,
  error: AlertTriangle,
};

const typeStyleMap = {
  info: "text-info bg-info-bg",
  success: "text-success bg-success-bg",
  warning: "text-warning bg-warning-bg",
  error: "text-danger bg-danger-bg",
};

export function NotificationItem({
  title,
  message,
  timestamp,
  isRead = false,
  type = "info",
  onClick,
}: NotificationItemProps) {
  const Icon = typeIconMap[type];

  return (
    <div
      onClick={onClick}
      className={`flex items-start gap-3 p-3 rounded-lg border border-border transition-colors ${
        isRead ? "bg-card text-muted-foreground" : "bg-secondary/60 text-foreground font-medium"
      } ${onClick ? "hover:bg-secondary cursor-pointer" : ""}`}
    >
      <div className={`p-2 rounded-md shrink-0 ${typeStyleMap[type]}`}>
        <Icon className="w-4 h-4" />
      </div>
      <div className="flex-1 space-y-0.5 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <p className="text-xs font-semibold text-foreground truncate">{title}</p>
          <span className="text-[10px] text-muted-foreground shrink-0 font-mono">{timestamp}</span>
        </div>
        <p className="text-xs text-muted-foreground line-clamp-2">{message}</p>
      </div>
      {!isRead && <span className="w-2 h-2 rounded-full bg-primary shrink-0 self-center" />}
    </div>
  );
}
