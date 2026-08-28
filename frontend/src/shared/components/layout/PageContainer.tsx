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
  children: React.ReactNode;
}

export function PageContainer({
  title,
  description,
  actions,
  children,
}: PageContainerProps) {
  return (
    <div className="w-full space-y-6 animate-in fade-in duration-200">
      {/* Page Header Section */}
      {(title || description || actions) && (
        <div className="space-y-3 pb-4 border-b border-outline-variant">
          {/* Breadcrumb Row */}
          <Breadcrumbs />

          {/* Title & Actions Row */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              {title && (
                <h1 className="type-headline-small text-foreground">
                  {title}
                </h1>
              )}
              {description && (
                <p className="type-body-medium text-on-surface-variant mt-1">
                  {description}
                </p>
              )}
            </div>

            {actions && (
              <div className="flex items-center gap-2 flex-wrap">
                {actions}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Main Page Children */}
      <div className="w-full space-y-6">{children}</div>
    </div>
  );
}
