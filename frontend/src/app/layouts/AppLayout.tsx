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
import { AppShell } from "@/shared/components/layout/AppShell";
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
      <>
        <CommandMenu />
        <AppShell
          navigation={<Sidebar
          isCollapsed={isCollapsed}
          onToggleCollapse={handleToggleCollapse}
          isOpenMobile={isOpenMobile}
          onCloseMobile={() => setIsOpenMobile(false)}
          />}
          topbar={<Topbar onOpenMobileNav={() => setIsOpenMobile(true)} />}
          beforeContent={<Breadcrumbs />}
        >
          <Outlet />
        </AppShell>
      </>
    </BreadcrumbProvider>
  );
}
