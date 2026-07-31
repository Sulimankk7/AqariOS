/**
 * DashboardError Component — Inline error state using pure semantic classes.
 */

import React from "react";
import { AlertCircle, RefreshCw } from "lucide-react";

interface DashboardErrorProps {
  message?: string;
  onRetry: () => void;
}

export function DashboardError({ message, onRetry }: DashboardErrorProps) {
  return (
    <div className="w-full p-6 rounded-lg border border-border bg-card flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
      <div className="flex items-start gap-3">
        <AlertCircle className="w-5 h-5 text-danger mt-0.5 shrink-0" />
        <div>
          <h3 className="text-sm font-semibold text-foreground">
            Failed to load dashboard summary
          </h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            {message || "An error occurred while fetching metrics from the server."}
          </p>
        </div>
      </div>
      <button
        onClick={onRetry}
        className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-md text-xs font-medium bg-primary text-primary-foreground hover:opacity-90 transition-opacity focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring shrink-0 cursor-pointer"
      >
        <RefreshCw className="w-3.5 h-3.5" />
        Retry Request
      </button>
    </div>
  );
}
