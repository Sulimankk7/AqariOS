import React from "react";
import { NavLink, useLocation } from "react-router";
import { LayoutDashboard, UserCircle, X } from "lucide-react";
import { useTranslation } from "@/shared/i18n";

export interface TenantSidebarProps {
  isOpenMobile: boolean;
  onCloseMobile: () => void;
}

export function TenantSidebar({ isOpenMobile, onCloseMobile }: TenantSidebarProps) {
  const { t } = useTranslation();
  const location = useLocation();

  const navItems = [
    {
      id: "dashboard",
      label: t("tenant.navigation.dashboard", "Dashboard"),
      path: "/tenant/dashboard",
      icon: LayoutDashboard,
    },
    {
      id: "profile",
      label: t("tenant.navigation.profile", "My Profile"),
      path: "/tenant/profile",
      icon: UserCircle,
    },
  ];

  const sidebarContent = (
    <aside
      aria-label="Tenant navigation"
      className="h-full flex flex-col bg-card border-e border-border z-40 select-none w-64"
    >
      <div className="h-14 px-3 flex items-center justify-between border-b border-border shrink-0">
        <div className="flex items-center gap-2.5 overflow-hidden">
          <div className="w-7 h-7 rounded-md bg-brand-green-900 text-white flex items-center justify-center font-bold font-mono text-[11px] shadow-xs shrink-0">
            AQ
          </div>
          <div className="overflow-hidden">
            <span className="font-bold tracking-tight text-foreground text-sm block truncate leading-tight">
              {t("common.appName", "AqariOS")}
            </span>
            <span className="text-[9px] text-muted-foreground block truncate leading-tight">
              {t("tenant.portal", "Tenant Portal")}
            </span>
          </div>
        </div>
        <button
          onClick={onCloseMobile}
          aria-label="Close navigation"
          className="flex lg:hidden items-center justify-center w-6 h-6 rounded-md text-muted-foreground hover:bg-secondary cursor-pointer"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      </div>

      <nav aria-label="Tenant modules" className="flex-1 overflow-y-auto p-2 space-y-1 mt-2">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive =
            location.pathname === item.path ||
            (item.path !== "/" && location.pathname.startsWith(item.path + "/"));

          return (
            <NavLink
              key={item.id}
              to={item.path}
              onClick={onCloseMobile}
              aria-label={item.label}
              aria-current={isActive ? "page" : undefined}
              className={`flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-xs font-medium transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
                isActive
                  ? "bg-primary text-primary-foreground font-semibold shadow-xs"
                  : "text-muted-foreground hover:text-foreground hover:bg-secondary"
              }`}
            >
              <Icon
                className={`w-4 h-4 shrink-0 ${
                  isActive ? "text-primary-foreground" : "text-brand-green-600"
                }`}
              />
              <span className="truncate">{item.label}</span>
            </NavLink>
          );
        })}
      </nav>
    </aside>
  );

  return (
    <>
      {/* Desktop */}
      <div className="hidden lg:block h-screen sticky top-0 shrink-0" aria-hidden={false}>
        {sidebarContent}
      </div>

      {/* Mobile */}
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
