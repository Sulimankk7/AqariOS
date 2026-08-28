/**
 * Feedback Components — EmptyState, ErrorState, and Skeleton Loaders.
 */

import React from "react";
import { FolderOpen, AlertCircle, RefreshCw } from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { Button } from "@/shared/ui/button";

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
    <div className="w-full flex min-h-48 flex-col items-center justify-center p-10 text-center space-y-4">
      <div className="flex size-12 items-center justify-center rounded-full bg-surface-container-high text-on-surface-variant">
        <Icon className="w-8 h-8" />
      </div>
      <div className="max-w-md space-y-1">
        <h3 className="type-title-medium text-foreground">
          {title || t("table.emptyState")}
        </h3>
        {description && <p className="type-body-medium text-on-surface-variant">{description}</p>}
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
    <div role="alert" className="w-full flex min-h-48 flex-col items-center justify-center p-8 text-center rounded-md bg-error-container/55 space-y-4">
      <div className="p-3 rounded-full bg-danger-bg text-danger">
        <AlertCircle className="w-6 h-6" />
      </div>
      <div className="max-w-md space-y-1">
        <h3 className="type-title-medium text-destructive">
          {title || t("errors.generic")}
        </h3>
        {message && <p className="type-body-medium text-on-surface-variant">{message}</p>}
      </div>
      {onRetry && (
        <Button
          onClick={onRetry}
          variant="outlined"
          size="sm"
        >
          <RefreshCw className="w-3.5 h-3.5" />
          <span>{t("common.retry")}</span>
        </Button>
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
      className={`animate-pulse rounded-xs bg-surface-container-highest ${className}`}
      aria-hidden="true"
    />
  );
}
