import { Topbar } from "@/shared/components/layout/Topbar";

export function PlatformAdminTopbar({ onOpenNavigation }: { onOpenNavigation: () => void }) {
  return <Topbar portal="platform" onOpenMobileNav={onOpenNavigation} />;
}
