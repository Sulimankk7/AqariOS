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
  default: "border-outline-variant bg-surface-container-high text-primary",
  info: "border-info/30 bg-info-bg text-info",
  success: "border-success/30 bg-success-bg text-success",
  warning: "border-warning/30 bg-warning-bg text-warning",
  danger: "border-danger/30 bg-danger-bg text-danger",
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
      className={`group relative flex flex-col justify-between p-5 rounded-lg border border-outline-variant bg-card shadow-e0 transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
        path ? "hover:border-outline hover:shadow-e1 cursor-pointer" : ""
      }`}
    >
      {/* Top Header: Title & Icon */}
      <div className="flex items-center justify-between gap-3 mb-4">
        <h3 className="type-label-large text-on-surface-variant line-clamp-1">
          {title}
        </h3>
        <div className="flex items-center gap-1.5">
          <div
            className={`p-2 rounded-md border shrink-0 transition-colors ${variantStyleMap[variant]}`}
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
        <div className="type-headline-small font-bold font-mono tracking-tight text-foreground">
          {value}
        </div>
        {description && (
          <p className="type-body-small text-muted-foreground">
            {description}
          </p>
        )}
      </div>
    </article>
  );
}
