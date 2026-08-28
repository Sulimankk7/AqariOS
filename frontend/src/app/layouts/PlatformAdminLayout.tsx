import { useState } from "react";
import { Outlet } from "react-router";
import { PlatformAdminSidebar } from "@/features/platformAdmin/components/PlatformAdminSidebar";
import { PlatformAdminTopbar } from "@/features/platformAdmin/components/PlatformAdminTopbar";
import { AppShell } from "@/shared/components/layout/AppShell";

export function PlatformAdminLayout() {
  const [open, setOpen] = useState(false);
  return <AppShell
    navigation={<PlatformAdminSidebar open={open} onClose={() => setOpen(false)} />}
    topbar={<PlatformAdminTopbar onOpenNavigation={() => setOpen(true)} />}
  >
    <Outlet />
  </AppShell>;
}
