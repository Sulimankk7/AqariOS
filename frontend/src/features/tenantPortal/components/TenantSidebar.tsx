import { CreditCard, FileText, LayoutDashboard, UserCircle, Zap } from "lucide-react";
import { ROUTES } from "@/config/routes";
import { NavigationSidebar, type NavigationGroup } from "@/shared/components/layout/NavigationSidebar";
import { useTranslation } from "@/shared/i18n";

export interface TenantSidebarProps { isOpenMobile: boolean; onCloseMobile: () => void; }

export function TenantSidebar({ isOpenMobile, onCloseMobile }: TenantSidebarProps) {
  const { t } = useTranslation();
  const groups: NavigationGroup[] = [{ id: "tenant", items: [
    { id: "dashboard", label: t("tenant.navigation.dashboard"), path: ROUTES.tenant.dashboard, icon: LayoutDashboard },
    { id: "lease", label: t("tenant.navigation.lease"), path: ROUTES.tenant.lease, icon: FileText },
    { id: "payments", label: t("tenant.navigation.payments"), path: ROUTES.tenant.payments, icon: CreditCard },
    { id: "bills", label: t("tenant.navigation.bills"), path: ROUTES.tenant.bills, icon: Zap },
    { id: "profile", label: t("tenant.navigation.profile"), path: ROUTES.tenant.profile, icon: UserCircle },
  ] }];

  return <NavigationSidebar
    identity={{ title: t("common.appName"), subtitle: t("tenant.portal") }}
    groups={groups}
    mobileOpen={isOpenMobile}
    onCloseMobile={onCloseMobile}
    ariaLabel={t("common.tenantNavigation")}
  />;
}
