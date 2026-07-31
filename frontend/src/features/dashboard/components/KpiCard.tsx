/**
 * KpiCard Component — Enterprise SaaS metric card.
 * Consumes pure semantic CSS variable classes (bg-card, border-border, text-foreground).
 * Navigates to related module on click.
 */

import React from "react";
import { useNavigate } from "react-router";
import { LucideIcon, ArrowUpRight } from "lucide-react";

export interface KpiCardProps {
  title: string;
  value: string | number;
  description?: string;
  icon: LucideIcon;
  variant?: "default" | "success" | "warning" | "danger" | "info";
  path?: string;
  ariaLabel?: string;
}

const variantStyleMap = {
  default: "bg-secondary text-secondary-foreground",
  info: "bg-info-bg text-info",
  success: "bg-success-bg text-success",
  warning: "bg-warning-bg text-warning",
  danger: "bg-danger-bg text-danger",
};

export function KpiCard({
  title,
  value,
  description,
  icon: Icon,
  variant = "default",
  path,
  ariaLabel,
}: KpiCardProps) {
  const navigate = useNavigate();

  const handleClick = () => {
    if (path) {
      navigate(path);
    }
  };

  return (
    <article
      tabIndex={0}
      role={path ? "button" : "article"}
      onClick={handleClick}
      onKeyDown={(e) => {
        if (path && (e.key === "Enter" || e.key === " ")) {
          e.preventDefault();
          handleClick();
        }
      }}
      aria-label={ariaLabel || `${title}: ${value}`}
      className={`group relative flex flex-col justify-between p-5 rounded-lg border border-border bg-card transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
        path ? "hover:border-border-strong hover:shadow-md cursor-pointer" : ""
      }`}
    >
      {/* Top Header: Title & Icon */}
      <div className="flex items-center justify-between gap-3 mb-4">
        <h3 className="text-sm font-medium text-muted-foreground tracking-tight line-clamp-1">
          {title}
        </h3>
        <div className="flex items-center gap-1.5">
          <div
            className={`p-2 rounded-md shrink-0 transition-colors ${variantStyleMap[variant]}`}
            aria-hidden="true"
          >
            <Icon className="w-4 h-4" strokeWidth={2} />
          </div>
          {path && (
            <ArrowUpRight className="w-4 h-4 text-muted-foreground/0 group-hover:text-muted-foreground transition-all duration-200 rtl:rotate-270" />
          )}
        </div>
      </div>

      {/* Main Metric & Subtitle */}
      <div className="space-y-1">
        <div className="text-2xl font-bold font-mono tracking-tight text-foreground">
          {value}
        </div>
        {description && (
          <p className="text-xs text-muted-foreground font-normal">
            {description}
          </p>
        )}
      </div>
    </article>
  );
}
