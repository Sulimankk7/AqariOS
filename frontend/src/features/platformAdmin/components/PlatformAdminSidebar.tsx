import { ClipboardList, CreditCard, Layers3, LayoutDashboard, MessageSquareText } from "lucide-react";
import { ROUTES } from "@/config/routes";
import { NavigationSidebar, type NavigationGroup } from "@/shared/components/layout/NavigationSidebar";
import { useTranslation } from "@/shared/i18n";

export function PlatformAdminSidebar({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation();
  const groups: NavigationGroup[] = [{ id: "platform", items: [
    { id: "dashboard", label: t("platformAdmin.nav.dashboard"), path: ROUTES.platform.dashboard, icon: LayoutDashboard },
    { id: "registrations", label: t("platformAdmin.nav.registrations"), path: ROUTES.platform.landlordRegistrations, icon: ClipboardList },
    { id: "contact-requests", label: t("platformAdmin.nav.contactRequests"), path: ROUTES.platform.contactRequests, icon: MessageSquareText },
    { id: "plans", label: t("platformAdmin.nav.plans"), path: ROUTES.platform.plans, icon: Layers3 },
    { id: "subscriptions", label: t("platformAdmin.nav.subscriptions"), path: ROUTES.platform.subscriptions, icon: CreditCard },
    { id: "change-requests", label: t("subscriptions.platformRequests"), path: ROUTES.platform.planChangeRequests, icon: ClipboardList },
  ] }];

  return <NavigationSidebar
    identity={{ title: t("common.appName"), subtitle: t("platformAdmin.title") }}
    groups={groups}
    mobileOpen={open}
    onCloseMobile={onClose}
    ariaLabel={t("platformAdmin.title")}
  />;
}
