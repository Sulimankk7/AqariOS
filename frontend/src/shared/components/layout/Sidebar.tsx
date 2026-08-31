import { Bell, Building2, Calculator, Car, CreditCard, FileText, FolderOpen, Home, LayoutDashboard, ReceiptText, Settings, Users, Wallet } from "lucide-react";
import { ROUTES } from "@/config/routes";
import { useTranslation } from "@/shared/i18n";
import { NavigationSidebar, type NavigationGroup } from "./NavigationSidebar";

export interface SidebarProps {
  isCollapsed: boolean;
  onToggleCollapse: () => void;
  isOpenMobile: boolean;
  onCloseMobile: () => void;
}

export function Sidebar({ isCollapsed, onToggleCollapse, isOpenMobile, onCloseMobile }: SidebarProps) {
  const { t } = useTranslation();
  const groups: NavigationGroup[] = [
    { id: "main", label: t("nav.groupMain"), items: [{ id: "dashboard", label: t("nav.dashboard"), path: ROUTES.dashboard.root, icon: LayoutDashboard }] },
    { id: "assets", label: t("nav.groupAssets"), items: [
      { id: "buildings", label: t("nav.buildings"), path: ROUTES.buildings.root, icon: Building2 },
      { id: "apartments", label: t("nav.apartments"), path: ROUTES.apartments.root, icon: Home },
      { id: "parking", label: t("nav.parking"), path: ROUTES.parking.root, icon: Car },
    ] },
    { id: "leasing", label: t("nav.groupLeasing"), items: [
      { id: "leases", label: t("nav.leases"), path: ROUTES.leases.root, icon: FileText },
      { id: "tenants", label: t("nav.tenants"), path: ROUTES.tenants.root, icon: Users },
    ] },
    { id: "finance", label: t("nav.groupFinance"), items: [
      { id: "payments", label: t("nav.payments"), path: ROUTES.payments.root, icon: Wallet },
      { id: "financial-operations", label: t("nav.financialOps"), path: ROUTES.financialOperations.root, icon: Calculator },
      { id: "utility-bills", label: t("nav.utilityBills"), path: ROUTES.utilityBills.root, icon: ReceiptText },
    ] },
    { id: "services", label: t("nav.groupServices"), items: [
      { id: "subscriptions", label: t("subscriptions.title"), path: ROUTES.subscriptions.root, icon: CreditCard },
    ] },
    { id: "operations", label: t("nav.groupOperations"), items: [
      { id: "documents", label: t("nav.documents"), path: ROUTES.documents.root, icon: FolderOpen },
      { id: "notifications", label: t("nav.notifications"), path: ROUTES.notifications.root, icon: Bell },
      { id: "settings", label: t("nav.settings"), path: ROUTES.settings.root, icon: Settings },
    ] },
  ];

  return <NavigationSidebar
    identity={{ title: t("common.appName"), subtitle: t("common.tagline") }}
    groups={groups}
    mobileOpen={isOpenMobile}
    onCloseMobile={onCloseMobile}
    collapsed={isCollapsed}
    onToggleCollapsed={onToggleCollapse}
    footer={<div className="flex items-center justify-between type-label-small text-muted-foreground"><span>v1.0.0 {t("common.enterpriseEdition")}</span><span className="size-2 rounded-full bg-success" aria-label={t("common.systemOnline")} /></div>}
  />;
}
