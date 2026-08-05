/**
 * Breadcrumbs Component — Dynamic Accessible Breadcrumb Navigation.
 * Formats route paths into breadcrumb segments with RTL support and entity name resolution.
 */

import React from "react";
import { useLocation, Link } from "react-router";
import { ChevronRight, Home } from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { useBreadcrumbTitles } from "./BreadcrumbContext";

export interface BreadcrumbsProps {
  customSegments?: Array<{ label: string; href?: string; title?: string }>;
}

const GUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function Breadcrumbs({ customSegments }: BreadcrumbsProps) {
  const location = useLocation();
  const { t } = useTranslation();
  const { breadcrumbTitles } = useBreadcrumbTitles();

  let segments: Array<{ label: string; href?: string; title?: string }> = [];

  if (customSegments) {
    segments = customSegments;
  } else {
    const pathnames = location.pathname.split("/").filter(Boolean);
    segments = pathnames.map((name, index) => {
      const href = `/${pathnames.slice(0, index + 1).join("/")}`;
      
      let label = name;
      let title: string | undefined = undefined;

      if (GUID_REGEX.test(name)) {
        title = `ID: ${name}`;
        label = breadcrumbTitles[name] || "Details";
      } else {
        const translationKey = `nav.${name}`;
        label = t(translationKey) !== translationKey ? t(translationKey) : name.charAt(0).toUpperCase() + name.slice(1);
      }

      return { label, href, title };
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
              <span 
                className="text-foreground font-semibold truncate max-w-40 cursor-default" 
                title={segment.title || segment.label}
              >
                {segment.label}
              </span>
            ) : (
              <Link
                to={segment.href}
                className="hover:text-foreground transition-colors truncate max-w-40 cursor-pointer"
                title={segment.title || segment.label}
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
