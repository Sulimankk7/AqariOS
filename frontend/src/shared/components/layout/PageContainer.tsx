/**
 * PageContainer Component — Reusable Accessible Page Shell.
 * Provides standardized page headers, breadcrumbs, action buttons, and responsive grid padding.
 */

import React from "react";
import { Breadcrumbs } from "./Breadcrumbs";

export interface PageContainerProps {
  title?: string;
  description?: string;
  actions?: React.ReactNode;
  showBreadcrumbs?: boolean;
  children: React.ReactNode;
}

export function PageContainer({
  title,
  description,
  actions,
  showBreadcrumbs = true,
  children,
}: PageContainerProps) {
  return (
    <div className="w-full min-w-0 max-w-full space-y-6 animate-in fade-in duration-200">
      {/* Page Header Section */}
      {(title || description || actions) && (
        <div className="space-y-3 pb-4 border-b border-outline-variant">
          {/* Breadcrumb Row */}
          {showBreadcrumbs && <Breadcrumbs />}

          {/* Title & Actions Row */}
          <div className="flex min-w-0 flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="min-w-0">
              {title && (
                <h1 className="type-headline-small break-words text-foreground">
                  {title}
                </h1>
              )}
              {description && (
                <p className="type-body-medium mt-1 break-words text-on-surface-variant">
                  {description}
                </p>
              )}
            </div>

            {actions && (
              <div className="flex max-w-full flex-wrap items-center gap-2">
                {actions}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Main Page Children */}
      <div className="w-full min-w-0 max-w-full space-y-6">{children}</div>
    </div>
  );
}
