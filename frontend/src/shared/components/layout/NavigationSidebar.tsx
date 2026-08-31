import React, { useEffect, useState } from "react";
import { NavLink, useLocation } from "react-router";
import { ChevronDown, ChevronLeft, ChevronRight, X } from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { cn } from "@/shared/ui/utils";
import { AqariOSLogo } from "@/shared/components/AqariOSLogo";

export interface NavigationItem {
  id: string;
  label: string;
  path: string;
  icon: React.ComponentType<{ className?: string }>;
}

export interface NavigationGroup {
  id: string;
  label?: string;
  items: NavigationItem[];
}

export interface NavigationIdentity {
  title: string;
  subtitle?: string;
  mark?: React.ReactNode;
}

export interface NavigationSidebarProps {
  identity: NavigationIdentity;
  groups: NavigationGroup[];
  mobileOpen: boolean;
  onCloseMobile: () => void;
  collapsed?: boolean;
  onToggleCollapsed?: () => void;
  footer?: React.ReactNode;
  ariaLabel?: string;
}

export function NavigationSidebar({
  identity,
  groups,
  mobileOpen,
  onCloseMobile,
  collapsed = false,
  onToggleCollapsed,
  footer,
  ariaLabel,
}: NavigationSidebarProps) {
  const { t } = useTranslation();
  const location = useLocation();
  const [expanded, setExpanded] = useState<Record<string, boolean>>(() =>
    Object.fromEntries(groups.map((group) => [group.id, true])),
  );

  useEffect(() => {
    setExpanded((current) => {
      const next = { ...current };
      for (const group of groups) {
        if (group.items.some((item) => location.pathname === item.path || location.pathname.startsWith(`${item.path}/`))) {
          next[group.id] = true;
        }
      }
      return next;
    });
  }, [location.pathname]);

  const content = (compact: boolean, mobile: boolean) => (
    <aside
      aria-label={ariaLabel ?? t("common.mainNavigation")}
      data-slot="navigation-sidebar"
      data-collapsed={compact || undefined}
      className={cn(
        "flex h-full min-h-0 flex-col border-e border-sidebar-border bg-sidebar text-foreground shadow-e1 transition-[width] duration-200",
        compact ? "w-[4.5rem]" : "w-72",
      )}
    >
      <div className={cn("flex h-16 shrink-0 items-center border-b border-sidebar-border", compact ? "justify-center px-2" : "justify-between px-4")}>
        <div className={cn("flex min-w-0 items-center", compact ? "justify-center" : "gap-3")}>
          <div className="flex size-9 shrink-0 items-center justify-center rounded-sm">
            {identity.mark ?? <AqariOSLogo size={28} alt="" />}
          </div>
          {!compact && (
            <div className="min-w-0">
              <span className="block truncate type-title-small text-foreground">{identity.title}</span>
              {identity.subtitle && <span className="block truncate type-label-small text-on-surface-variant">{identity.subtitle}</span>}
            </div>
          )}
        </div>

        {mobile ? (
          <button type="button" onClick={onCloseMobile} aria-label={t("common.closeNavigation")} className="grid size-10 place-items-center rounded-full text-muted-foreground hover:bg-surface-container-high hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <X className="size-4" />
          </button>
        ) : onToggleCollapsed ? (
          <button type="button" onClick={onToggleCollapsed} aria-label={compact ? t("common.expandSidebar") : t("common.collapseSidebar")} className="grid size-8 place-items-center rounded-full text-on-surface-variant hover:bg-surface-container-high hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            {compact ? <ChevronRight className="size-4 rtl:rotate-180" /> : <ChevronLeft className="size-4 rtl:rotate-180" />}
          </button>
        ) : null}
      </div>

      <nav className={cn("min-h-0 flex-1 overflow-y-auto overscroll-contain py-3", compact ? "px-2" : "px-3")}>
        <div className="space-y-3">
          {groups.map((group) => {
            const isExpanded = expanded[group.id] !== false;
            return (
              <section key={group.id} aria-label={group.label} className="space-y-1">
                {!compact && group.label && (
                  <button
                    type="button"
                    onClick={() => setExpanded((current) => ({ ...current, [group.id]: !isExpanded }))}
                    aria-expanded={isExpanded}
                    className="flex min-h-8 w-full items-center justify-between rounded-sm px-3 text-start type-label-small uppercase text-on-surface-variant hover:bg-surface-container-high focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  >
                    <span className="truncate">{group.label}</span>
                    <ChevronDown className={cn("size-3.5 shrink-0 transition-transform", !isExpanded && "-rotate-90 rtl:rotate-90")} />
                  </button>
                )}

                {(compact || isExpanded) && (
                  <div className="space-y-1">
                    {group.items.map((item) => {
                      const Icon = item.icon;
                      return (
                        <NavLink
                          key={item.id}
                          to={item.path}
                          onClick={mobile ? onCloseMobile : undefined}
                          title={compact ? item.label : undefined}
                          aria-label={item.label}
                          className={({ isActive }) => cn(
                            "group relative flex min-h-11 items-center rounded-sm px-3 type-label-large transition-[background-color,color,box-shadow] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                            compact ? "justify-center" : "gap-3",
                            isActive
                              ? "bg-primary-container text-on-primary-container shadow-e1"
                              : "text-on-surface-variant hover:bg-surface-container-high hover:text-foreground",
                          )}
                        >
                          {({ isActive }) => (
                            <>
                              <Icon className={cn("size-[1.125rem] shrink-0", isActive ? "text-on-primary-container" : "text-primary")} />
                              {!compact && <span className="min-w-0 flex-1 truncate">{item.label}</span>}
                              {isActive && <span aria-hidden className="absolute inset-y-2 start-0 w-0.5 rounded-full bg-primary" />}
                            </>
                          )}
                        </NavLink>
                      );
                    })}
                  </div>
                )}
              </section>
            );
          })}
        </div>
      </nav>

      {!compact && footer && <div className="shrink-0 border-t border-sidebar-border p-3">{footer}</div>}
    </aside>
  );

  return (
    <>
      <div className="sticky top-0 hidden h-screen shrink-0 lg:block">{content(collapsed, false)}</div>
      {mobileOpen && (
        <div className="fixed inset-0 z-50 flex bg-scrim/55 backdrop-blur-[2px] lg:hidden" role="dialog" aria-modal="true" aria-label={t("common.navigationMenu")} onMouseDown={(event) => event.target === event.currentTarget && onCloseMobile()}>
          <div className="h-full max-w-[calc(100vw-2.5rem)] animate-in slide-in-from-start duration-200" onMouseDown={(event) => event.stopPropagation()}>
            {content(false, true)}
          </div>
        </div>
      )}
    </>
  );
}
