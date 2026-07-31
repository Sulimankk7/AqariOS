/**
 * Breadcrumbs Component — Dynamic Accessible Breadcrumb Navigation.
 * Formats route paths into breadcrumb segments with RTL support.
 */

import React from "react";
import { useLocation, Link } from "react-router";
import { ChevronRight, Home } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface BreadcrumbsProps {
  customSegments?: Array<{ label: string; href?: string }>;
}

export function Breadcrumbs({ customSegments }: BreadcrumbsProps) {
  const location = useLocation();
  const { t } = useTranslation();

  let segments: Array<{ label: string; href?: string }> = [];

  if (customSegments) {
    segments = customSegments;
  } else {
    const pathnames = location.pathname.split("/").filter(Boolean);
    segments = pathnames.map((name, index) => {
      const href = `/${pathnames.slice(0, index + 1).join("/")}`;
      const translationKey = `nav.${name}`;
      const label = t(translationKey) !== translationKey ? t(translationKey) : name.charAt(0).toUpperCase() + name.slice(1);
      return { label, href };
    });
  }

  return (
    <nav aria-label="Breadcrumb" className="flex items-center gap-1.5 text-xs text-muted-foreground font-medium flex-wrap">
      <Link
        to="/dashboard"
        className="flex items-center gap-1 hover:text-foreground transition-colors cursor-pointer"
      >
        <Home className="w-3.5 h-3.5 shrink-0" />
        <span>{t("common.appName")}</span>
      </Link>

      {segments.map((segment, idx) => {
        const isLast = idx === segments.length - 1;

        return (
          <React.Fragment key={idx}>
            <ChevronRight className="w-3.5 h-3.5 rtl:rotate-180 text-muted-foreground/60 shrink-0" />
            {isLast || !segment.href ? (
              <span className="text-foreground font-semibold truncate max-w-40">{segment.label}</span>
            ) : (
              <Link
                to={segment.href}
                className="hover:text-foreground transition-colors truncate max-w-40 cursor-pointer"
              >
                {segment.label}
              </Link>
            )}
          </React.Fragment>
        );
      })}
    </nav>
  );
}
