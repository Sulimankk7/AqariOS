/**
 * Feedback Components — EmptyState, ErrorState, and Skeleton Loaders.
 */

import React from "react";
import { FolderOpen, AlertCircle, RefreshCw } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface EmptyStateProps {
  title?: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ComponentType<{ className?: string }>;
}

export function EmptyState({
  title,
  description,
  action,
  icon: Icon = FolderOpen,
}: EmptyStateProps) {
  const { t } = useTranslation();

  return (
    <div className="w-full flex flex-col items-center justify-center p-12 text-center rounded-xl border border-dashed border-border bg-card space-y-4">
      <div className="p-4 rounded-full bg-secondary text-muted-foreground">
        <Icon className="w-8 h-8" />
      </div>
      <div className="max-w-md space-y-1">
        <h3 className="text-base font-bold text-foreground">
          {title || t("table.emptyState")}
        </h3>
        {description && <p className="text-xs text-muted-foreground">{description}</p>}
      </div>
      {action}
    </div>
  );
}

export interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
}

export function ErrorState({ title, message, onRetry }: ErrorStateProps) {
  const { t } = useTranslation();

  return (
    <div className="w-full flex flex-col items-center justify-center p-8 text-center rounded-xl border border-danger/30 bg-danger-bg/50 space-y-4">
      <div className="p-3 rounded-full bg-danger-bg text-danger">
        <AlertCircle className="w-6 h-6" />
      </div>
      <div className="max-w-md space-y-1">
        <h3 className="text-base font-bold text-danger">
          {title || t("errors.generic")}
        </h3>
        {message && <p className="text-xs text-muted-foreground">{message}</p>}
      </div>
      {onRetry && (
        <button
          onClick={onRetry}
          className="flex items-center gap-2 px-4 py-2 rounded-lg bg-danger text-danger-foreground text-xs font-semibold hover:opacity-90 transition-opacity cursor-pointer"
        >
          <RefreshCw className="w-3.5 h-3.5" />
          <span>{t("common.retry")}</span>
        </button>
      )}
    </div>
  );
}

export interface SkeletonProps {
  className?: string;
}

export function Skeleton({ className = "" }: SkeletonProps) {
  return (
    <div
      className={`animate-pulse rounded-md bg-secondary/80 ${className}`}
      aria-hidden="true"
    />
  );
}
