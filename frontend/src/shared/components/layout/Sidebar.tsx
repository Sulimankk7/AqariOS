/**
 * Sidebar Component — Collapsible Enterprise Navigation Drawer.
 *
 * Features:
 * - Collapse / Expand with smooth transition
 * - Persisted collapsed state (via AuthProvider helpers — no direct localStorage)
 * - Auto-expand group containing the active route
 * - Tooltips for nav items in collapsed mode
 * - Keyboard accessible (all items are links / buttons)
 * - Responsive: sticky on desktop, overlay drawer on mobile
 */

import React, { useState, useEffect } from "react";
import { NavLink, useLocation } from "react-router";
import {
  LayoutDashboard,
  Building2,
  Home,
  Car,
  FileText,
  Users,
  Wallet,
  Calculator,
  Wrench,
  Store,
  FolderOpen,
  Bell,
  Settings,
  ReceiptText,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  X,
} from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { ROUTES } from "@/config/routes";

// ── Types ──────────────────────────────────────────────────────────────────────

export interface SidebarProps {
  isCollapsed: boolean;
  onToggleCollapse: () => void;
  isOpenMobile: boolean;
  onCloseMobile: () => void;
}

interface NavItem {
  id: string;
  label: string;
  labelKey?: string;
  path: string;
  icon: React.ComponentType<{ className?: string }>;
}

interface NavGroup {
  groupKey: string;
  groupLabel: string;
  items: NavItem[];
}

// ── Navigation structure — all 15 spec routes ──────────────────────────────────

const navGroups: NavGroup[] = [
  {
    groupKey: "nav.groupMain",
    groupLabel: "Main",
    items: [
      {
        id: "dashboard",
        label: "Dashboard",
        labelKey: "nav.dashboard",
        path: ROUTES.dashboard.root,
        icon: LayoutDashboard,
      },
    ],
  },
  {
    groupKey: "nav.groupAssets",
    groupLabel: "Property",
    items: [
      {
        id: "buildings",
        label: "Buildings",
        labelKey: "nav.buildings",
        path: ROUTES.buildings.root,
        icon: Building2,
      },
      {
        id: "apartments",
        label: "Apartments",
        labelKey: "nav.apartments",
        path: ROUTES.apartments.root,
        icon: Home,
      },
      {
        id: "parking",
        label: "Parking",
        labelKey: "nav.parking",
        path: ROUTES.parking.root,
        icon: Car,
      },
    ],
  },
  {
    groupKey: "nav.groupLeasing",
    groupLabel: "Leasing",
    items: [
      {
        id: "leases",
        label: "Leases",
        labelKey: "nav.leases",
        path: ROUTES.leases.root,
        icon: FileText,
      },
      {
        id: "tenants",
        label: "Tenants",
        labelKey: "nav.tenants",
        path: ROUTES.tenants.root,
        icon: Users,
      },
    ],
  },
  {
    groupKey: "nav.groupFinance",
    groupLabel: "Finance",
    items: [
      {
        id: "payments",
        label: "Payments",
        labelKey: "nav.payments",
        path: ROUTES.payments.root,
        icon: Wallet,
      },
      {
        id: "financialOps",
        label: "Financial Operations",
        labelKey: "nav.financialOps",
        path: ROUTES.financialOperations.root,
        icon: Calculator,
      },
      {
        id: "utilityBills",
        label: "Utility Bills",
        labelKey: "nav.utilityBills",
        path: ROUTES.utilityBills.root,
        icon: ReceiptText,
      },
    ],
  },
  {
    groupKey: "nav.groupOperations",
    groupLabel: "Operations",
    items: [
      {
        id: "maintenance",
        label: "Maintenance",
        labelKey: "nav.maintenance",
        path: ROUTES.maintenance.root,
        icon: Wrench,
      },
      {
        id: "marketplace",
        label: "Marketplace",
        labelKey: "nav.marketplace",
        path: ROUTES.marketplace.root,
        icon: Store,
      },
      {
        id: "documents",
        label: "Documents",
        labelKey: "nav.documents",
        path: ROUTES.documents.root,
        icon: FolderOpen,
      },
      {
        id: "notifications",
        label: "Notifications",
        labelKey: "nav.notifications",
        path: ROUTES.notifications.root,
        icon: Bell,
      },
      {
        id: "settings",
        label: "Settings",
        labelKey: "nav.settings",
        path: ROUTES.settings.root,
        icon: Settings,
      },
    ],
  },
];

// ── Component ──────────────────────────────────────────────────────────────────

export function Sidebar({
  isCollapsed,
  onToggleCollapse,
  isOpenMobile,
  onCloseMobile,
}: SidebarProps) {
  const { t } = useTranslation();
  const location = useLocation();

  // Tracks which groups are expanded. Default: all expanded.
  const [expandedGroups, setExpandedGroups] = useState<Record<number, boolean>>(() => {
    const initial: Record<number, boolean> = {};
    navGroups.forEach((_, idx) => { initial[idx] = true; });
    return initial;
  });

  // Auto-expand the group containing the active route whenever the route changes
  useEffect(() => {
    setExpandedGroups((prev) => {
      const next = { ...prev };
      navGroups.forEach((group, idx) => {
        const hasActiveChild = group.items.some((item) =>
          location.pathname === item.path || location.pathname.startsWith(item.path + "/")
        );
        if (hasActiveChild) {
          next[idx] = true;
        }
      });
      return next;
    });
  }, [location.pathname]);

  const toggleGroup = (idx: number) => {
    setExpandedGroups((prev) => ({ ...prev, [idx]: !prev[idx] }));
  };

  /** Resolve label: use i18n key if available, fall back to literal label */
  const getLabel = (item: NavItem): string => {
    if (item.labelKey) {
      const translated = t(item.labelKey);
      // If the key wasn't found, t() returns the key itself — use fallback label
      return translated !== item.labelKey ? translated : item.label;
    }
    return item.label;
  };

  /** Resolve group label */
  const getGroupLabel = (group: NavGroup): string => {
    const translated = t(group.groupKey);
    return translated !== group.groupKey ? translated : group.groupLabel;
  };

  // ── Sidebar DOM ─────────────────────────────────────────────────────────────

  const sidebarContent = (
    <aside
      aria-label="Main navigation"
      className={`h-full flex flex-col bg-card border-e border-border z-40 select-none transition-[width] duration-300 ease-in-out ${
        isCollapsed ? "w-[60px]" : "w-64"
      }`}
    >
      {/* Brand + Collapse Toggle */}
      <div className="h-14 px-3 flex items-center justify-between border-b border-border shrink-0">
        <div className="flex items-center gap-2.5 overflow-hidden">
          <div className="w-7 h-7 rounded-md bg-brand-green-900 text-white flex items-center justify-center font-bold font-mono text-[11px] shadow-xs shrink-0">
            AQ
          </div>
          {!isCollapsed && (
            <div className="overflow-hidden">
              <span className="font-bold tracking-tight text-foreground text-sm block truncate leading-tight">
                {t("common.appName")}
              </span>
              <span className="text-[9px] text-muted-foreground block truncate leading-tight">
                {t("common.tagline")}
              </span>
            </div>
          )}
        </div>

        {/* Desktop collapse toggle */}
        <button
          onClick={onToggleCollapse}
          aria-label={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
          className="hidden lg:flex items-center justify-center w-6 h-6 rounded-md border border-border text-muted-foreground hover:text-foreground hover:bg-secondary transition-colors cursor-pointer shrink-0"
        >
          {isCollapsed ? (
            <ChevronRight className="w-3.5 h-3.5 rtl:rotate-180" />
          ) : (
            <ChevronLeft className="w-3.5 h-3.5 rtl:rotate-180" />
          )}
        </button>

        {/* Mobile close button */}
        <button
          onClick={onCloseMobile}
          aria-label="Close navigation"
          className="flex lg:hidden items-center justify-center w-6 h-6 rounded-md text-muted-foreground hover:bg-secondary cursor-pointer"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Navigation */}
      <nav
        aria-label="Application modules"
        className="flex-1 overflow-y-auto p-2 space-y-1"
      >
        {navGroups.map((group, groupIdx) => {
          const isGroupExpanded = expandedGroups[groupIdx] !== false;

          return (
            <div key={group.groupKey}>
              {/* Group header — hidden when sidebar is collapsed */}
              {!isCollapsed && (
                <button
                  onClick={() => toggleGroup(groupIdx)}
                  aria-expanded={isGroupExpanded}
                  className="w-full flex items-center justify-between px-2.5 py-1 cursor-pointer hover:bg-secondary rounded text-[10px] font-semibold text-muted-foreground uppercase tracking-wider transition-colors mb-0.5"
                >
                  <span>{getGroupLabel(group)}</span>
                  <ChevronDown
                    className={`w-3 h-3 transition-transform duration-200 ${
                      isGroupExpanded ? "" : "-rotate-90"
                    }`}
                  />
                </button>
              )}

              {/* Group items — always show in collapsed mode, respect expand in expanded mode */}
              {(isGroupExpanded || isCollapsed) && (
                <div className="space-y-0.5">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const isActive =
                      location.pathname === item.path ||
                      (item.path !== "/" && location.pathname.startsWith(item.path + "/"));
                    const label = getLabel(item);

                    return (
                      <div key={item.id} className="relative group/item">
                        <NavLink
                          to={item.path}
                          onClick={onCloseMobile}
                          aria-label={label}
                          aria-current={isActive ? "page" : undefined}
                          className={`flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-xs font-medium transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
                            isActive
                              ? "bg-primary text-primary-foreground font-semibold shadow-xs"
                              : "text-muted-foreground hover:text-foreground hover:bg-secondary"
                          } ${isCollapsed ? "justify-center" : ""}`}
                        >
                          <Icon
                            className={`w-4 h-4 shrink-0 ${
                              isActive ? "text-primary-foreground" : "text-brand-green-600"
                            }`}
                          />
                          {!isCollapsed && (
                            <span className="truncate">{label}</span>
                          )}
                        </NavLink>

                        {/* Tooltip — only in collapsed mode */}
                        {isCollapsed && (
                          <div
                            role="tooltip"
                            className="absolute start-full ms-2 top-1/2 -translate-y-1/2 hidden group-hover/item:flex items-center px-2.5 py-1.5 bg-popover text-popover-foreground text-xs rounded-lg border border-border whitespace-nowrap shadow-md z-50 pointer-events-none"
                          >
                            {label}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </nav>

      {/* Footer */}
      {!isCollapsed && (
        <div className="p-3 border-t border-border text-[10px] text-muted-foreground flex items-center justify-between shrink-0">
          <span>v1.0.0 Enterprise</span>
          <span className="w-1.5 h-1.5 rounded-full bg-success" aria-label="System online" />
        </div>
      )}
    </aside>
  );

  return (
    <>
      {/* Desktop: sticky sidebar */}
      <div className="hidden lg:block h-screen sticky top-0 shrink-0" aria-hidden={false}>
        {sidebarContent}
      </div>

      {/* Mobile: overlay drawer */}
      {isOpenMobile && (
        <div
          className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm lg:hidden flex"
          onClick={onCloseMobile}
          aria-modal="true"
          role="dialog"
          aria-label="Navigation menu"
        >
          <div
            className="h-full w-64 animate-in slide-in-from-start duration-200"
            onClick={(e) => e.stopPropagation()}
          >
            {sidebarContent}
          </div>
        </div>
      )}
    </>
  );
}
