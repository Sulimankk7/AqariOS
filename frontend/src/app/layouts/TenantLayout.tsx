import React, { useState } from "react";
import { Outlet } from "react-router";
import { TenantSidebar } from "@/features/tenantPortal/components/TenantSidebar";
import { Topbar } from "@/shared/components/layout/Topbar";
import { BreadcrumbProvider } from "@/shared/components/layout/BreadcrumbContext";
import { AppShell } from "@/shared/components/layout/AppShell";

export function TenantLayout() {
  const [isOpenMobile, setIsOpenMobile] = useState(false);

  return (
    <BreadcrumbProvider>
      <AppShell
        navigation={<TenantSidebar
          isOpenMobile={isOpenMobile}
          onCloseMobile={() => setIsOpenMobile(false)}
        />}
        topbar={<Topbar portal="tenant" onOpenMobileNav={() => setIsOpenMobile(true)} />}
      >
        <Outlet />
      </AppShell>
    </BreadcrumbProvider>
  );
}
