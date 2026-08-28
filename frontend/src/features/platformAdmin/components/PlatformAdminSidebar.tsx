import { LayoutDashboard, ClipboardList, CreditCard, Layers3, X } from "lucide-react";
import { NavLink } from "react-router";
import { ROUTES } from "@/config/routes";
import { useTranslation } from "@/shared/i18n";

export function PlatformAdminSidebar({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation();
  const items = [
    { to: ROUTES.platform.dashboard, label: t("platformAdmin.nav.dashboard"), icon: LayoutDashboard },
    { to: ROUTES.platform.landlordRegistrations, label: t("platformAdmin.nav.registrations"), icon: ClipboardList },
    { to: ROUTES.platform.plans, label: t("platformAdmin.nav.plans"), icon: Layers3 },
    { to: ROUTES.platform.subscriptions, label: t("platformAdmin.nav.subscriptions"), icon: CreditCard },
    { to: ROUTES.platform.planChangeRequests, label: t("subscriptions.platformRequests"), icon: ClipboardList },
  ];
  const content = <aside className="flex h-full w-64 flex-col border-e border-border bg-card">
    <div className="flex h-14 items-center justify-between border-b border-border px-4">
      <div><strong className="text-sm">{t("common.appName")}</strong><span className="block text-[10px] text-muted-foreground">{t("platformAdmin.title")}</span></div>
      <button className="rounded-md p-2 lg:hidden" onClick={onClose} aria-label={t("common.close")}><X className="h-4 w-4" /></button>
    </div>
    <nav className="flex-1 space-y-1 p-3" aria-label={t("platformAdmin.title")}>
      {items.map(({ to, label, icon: Icon }) => <NavLink key={to} to={to} onClick={onClose} className={({ isActive }) => `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium ${isActive ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-secondary hover:text-foreground"}`}><Icon className="h-4 w-4" />{label}</NavLink>)}
    </nav>
  </aside>;
  return <><div className="hidden h-screen shrink-0 lg:block">{content}</div>{open && <div className="fixed inset-0 z-50 flex bg-black/50 lg:hidden" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>{content}</div>}</>;
}
