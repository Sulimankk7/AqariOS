/**
 * StatCard Component — Enterprise KPI metric stat card.
 * Supports values, change indicators, variant themes, and module navigation.
 */

import React from "react";
import { useNavigate } from "react-router";
import { LucideIcon, TrendingUp, TrendingDown, ArrowUpRight } from "lucide-react";

export interface StatCardProps {
  title: string;
  value: string | number;
  description?: string;
  change?: { value: string | number; isPositive: boolean };
  icon?: LucideIcon;
  variant?: "default" | "success" | "warning" | "danger" | "info";
  path?: string;
  onClick?: () => void;
  ariaLabel?: string;
}

const variantStyles = {
  default: "bg-secondary text-secondary-foreground",
  info: "bg-info-bg text-info",
  success: "bg-success-bg text-success",
  warning: "bg-warning-bg text-warning",
  danger: "bg-danger-bg text-danger",
};

export function StatCard({
  title,
  value,
  description,
  change,
  icon: Icon,
  variant = "default",
  path,
  onClick,
  ariaLabel,
}: StatCardProps) {
  const navigate = useNavigate();

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else if (path) {
      navigate(path);
    }
  };

  return (
    <article
      tabIndex={0}
      role={path || onClick ? "button" : "article"}
      onClick={handleClick}
      onKeyDown={(e) => {
        if ((path || onClick) && (e.key === "Enter" || e.key === " ")) {
          e.preventDefault();
          handleClick();
        }
      }}
      aria-label={ariaLabel || `${title}: ${value}`}
      className={`group relative flex flex-col justify-between p-5 rounded-lg border border-border bg-card transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
        path || onClick ? "hover:border-border-strong hover:shadow-md cursor-pointer" : ""
      }`}
    >
      <div className="flex items-center justify-between gap-3 mb-3">
        <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider line-clamp-1">
          {title}
        </h3>
        <div className="flex items-center gap-1.5">
          {Icon && (
            <div className={`p-2 rounded-md shrink-0 ${variantStyles[variant]}`}>
              <Icon className="w-4 h-4" />
            </div>
          )}
          {(path || onClick) && (
            <ArrowUpRight className="w-4 h-4 text-muted-foreground/0 group-hover:text-muted-foreground transition-all duration-200 rtl:rotate-270" />
          )}
        </div>
      </div>

      <div className="space-y-2">
        <div className="text-2xl font-bold font-mono tracking-tight text-foreground">
          {value}
        </div>

        {(description || change) && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground flex-wrap">
            {change && (
              <span
                className={`inline-flex items-center gap-1 font-semibold ${
                  change.isPositive ? "text-success" : "text-danger"
                }`}
              >
                {change.isPositive ? (
                  <TrendingUp className="w-3.5 h-3.5" />
                ) : (
                  <TrendingDown className="w-3.5 h-3.5" />
                )}
                <span>{change.value}</span>
              </span>
            )}
            {description && <span>{description}</span>}
          </div>
        )}
      </div>
    </article>
  );
}
