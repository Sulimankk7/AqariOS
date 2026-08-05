/**
 * AppLayout — Single authenticated application shell for AqariOS.
 *
 * Layout structure:
 * ┌──────────────────────────────────────────────────┐
 * │ Sidebar │ Topbar                                 │
 * │         ├──────────────────────────────────────  │
 * │         │ Breadcrumbs                            │
 * │         │ <Outlet /> — page content              │
 * └─────────┴────────────────────────────────────────┘
 *
 * Architecture Rules:
 * - Sidebar collapsed state is persisted via AuthProvider helpers (no raw localStorage here).
 * - This component is ONLY rendered inside ProtectedRoute — never on public pages.
 * - All authenticated pages use <Outlet /> from this layout via React Router nesting.
 */

import React, { useState } from "react";
import { Outlet } from "react-router";
import { Sidebar } from "@/shared/components/layout/Sidebar";
import { Topbar } from "@/shared/components/layout/Topbar";
import { CommandMenu } from "@/shared/components/layout/CommandMenu";
import { Breadcrumbs } from "@/shared/components/layout/Breadcrumbs";
import { BreadcrumbProvider } from "@/shared/components/layout/BreadcrumbContext";
import {
  getSidebarCollapsed,
  setSidebarCollapsed,
} from "@/features/auth/providers/AuthProvider";

export function AppLayout() {
  const [isCollapsed, setIsCollapsed] = useState<boolean>(getSidebarCollapsed);
  const [isOpenMobile, setIsOpenMobile] = useState(false);

  const handleToggleCollapse = () => {
    setIsCollapsed((prev) => {
      const next = !prev;
      setSidebarCollapsed(next);
      return next;
    });
  };

  return (
    <BreadcrumbProvider>
      <div className="min-h-screen bg-background text-foreground flex overflow-x-hidden transition-colors duration-200">
        {/* Global Command Palette — Ctrl+K */}
        <CommandMenu />

        {/* Collapsible Sidebar */}
        <Sidebar
          isCollapsed={isCollapsed}
          onToggleCollapse={handleToggleCollapse}
          isOpenMobile={isOpenMobile}
          onCloseMobile={() => setIsOpenMobile(false)}
        />

        {/* Main workspace */}
        <div className="flex-1 flex flex-col min-w-0 h-screen overflow-y-auto">
          {/* Sticky Topbar */}
          <Topbar onOpenMobileNav={() => setIsOpenMobile(true)} />

          {/* Page area */}
          <main id="main-content" className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto space-y-4">
            {/* Skip-to-content anchor target */}
            <a
              href="#main-content"
              className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:start-4 focus:z-50 focus:px-4 focus:py-2 focus:bg-primary focus:text-primary-foreground focus:rounded-lg"
            >
              Skip to content
            </a>

            {/* Auto-generated breadcrumbs from current route */}
            <Breadcrumbs />

            {/* Active page content via React Router nested route */}
            <Outlet />
          </main>
        </div>
      </div>
    </BreadcrumbProvider>
  );
}
