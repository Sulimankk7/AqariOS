import React, { useState } from "react";
import { Outlet } from "react-router";
import { TenantSidebar } from "@/features/tenantPortal/components/TenantSidebar";
import { Topbar } from "@/shared/components/layout/Topbar";
import { BreadcrumbProvider } from "@/shared/components/layout/BreadcrumbContext";

export function TenantLayout() {
  const [isOpenMobile, setIsOpenMobile] = useState(false);

  return (
    <BreadcrumbProvider>
      <div className="min-h-screen bg-background text-foreground flex overflow-x-hidden transition-colors duration-200">
        <TenantSidebar
          isOpenMobile={isOpenMobile}
          onCloseMobile={() => setIsOpenMobile(false)}
        />

        <div className="flex-1 flex flex-col min-w-0 h-screen overflow-y-auto">
          <Topbar onOpenMobileNav={() => setIsOpenMobile(true)} />

          <main id="main-content" className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto space-y-4">
            <a
              href="#main-content"
              className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:start-4 focus:z-50 focus:px-4 focus:py-2 focus:bg-primary focus:text-primary-foreground focus:rounded-lg"
            >
              Skip to content
            </a>

            <Outlet />
          </main>
        </div>
      </div>
    </BreadcrumbProvider>
  );
}
