import React, { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { Building2, Check, ChevronDown, Globe, LogOut, Menu, Monitor, Moon, Search, ShieldCheck, SlidersHorizontal, Sun, UserCircle } from "lucide-react";
import { ROUTES } from "@/config/routes";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";
import { useTenantProfile } from "@/features/tenantPortal/hooks/useTenantProfile";
import { useTranslation } from "@/shared/i18n";
import { useTheme } from "@/shared/theme";
import { cn } from "@/shared/ui/utils";

export type PortalContext = "company" | "tenant" | "platform";

export interface TopbarProps {
  onOpenMobileNav: () => void;
  portal?: PortalContext;
  showSearch?: boolean;
  showNotifications?: boolean;
}

export function Topbar({ onOpenMobileNav, portal = "company", showSearch = portal === "company", showNotifications = true }: TopbarProps) {
  const { t, language, setLanguage } = useTranslation();
  const { theme, setTheme } = useTheme();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { data: tenantProfile } = useTenantProfile();
  const headerRef = useRef<HTMLElement>(null);
  const [companyMenuOpen, setCompanyMenuOpen] = useState(false);
  const [themeMenuOpen, setThemeMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const isTenant = portal === "tenant";
  const isPlatform = portal === "platform";
  const displayName = isTenant && tenantProfile?.name ? tenantProfile.name : (user?.name ?? "");
  const displayEmail = isTenant && tenantProfile?.email ? tenantProfile.email : (user?.email ?? "");
  const avatarInitial = (displayName || "U").slice(0, 1).toUpperCase();
  const companyInfo = portal === "company" && user ? {
    name: user.companyName || t("common.organizationName", { name: user.name }),
    code: "AQ-ORG",
    avatar: (user.companyName || user.name || "A").slice(0, 1).toUpperCase(),
  } : null;

  useEffect(() => {
    const closeMenus = (event: MouseEvent) => {
      if (headerRef.current && !headerRef.current.contains(event.target as Node)) {
        setCompanyMenuOpen(false);
        setThemeMenuOpen(false);
        setUserMenuOpen(false);
      }
    };
    document.addEventListener("mousedown", closeMenus);
    return () => document.removeEventListener("mousedown", closeMenus);
  }, []);

  const closeOtherMenus = (keep: "company" | "theme" | "user") => {
    if (keep !== "company") setCompanyMenuOpen(false);
    if (keep !== "theme") setThemeMenuOpen(false);
    if (keep !== "user") setUserMenuOpen(false);
  };

  const toggleLanguage = () => setLanguage(language === "ar" ? "en" : "ar");
  const contextLabel = isPlatform ? t("platformAdmin.title") : isTenant ? t("tenant.portal") : companyInfo?.name;
  const ContextIcon = isPlatform || isTenant ? ShieldCheck : Building2;

  const themeOptions = [
    { key: "light" as const, label: t("theme.light"), icon: Sun },
    { key: "dark" as const, label: t("theme.dark"), icon: Moon },
    { key: "system" as const, label: t("theme.system"), icon: Monitor },
  ];

  return (
    <header ref={headerRef} role="banner" data-slot="topbar" data-portal={portal} className="sticky top-0 z-30 flex h-16 w-full shrink-0 items-center justify-between gap-2 border-b border-border-strong bg-topbar/95 px-2.5 shadow-e1 backdrop-blur-md sm:px-4 lg:px-5">
      <div className="flex min-w-0 items-center gap-2">
        <button type="button" onClick={onOpenMobileNav} aria-label={t("common.openNavigation")} className="grid size-10 shrink-0 place-items-center rounded-full text-muted-foreground hover:bg-secondary hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring lg:hidden">
          <Menu className="size-5" />
        </button>

        {companyInfo ? (
          <div className="relative min-w-0">
            <button type="button" onClick={() => { closeOtherMenus("company"); setCompanyMenuOpen((open) => !open); }} aria-label={t("common.switchOrganization")} aria-expanded={companyMenuOpen} className="flex h-10 max-w-[10rem] items-center gap-2 rounded-sm border border-border bg-surface-container-low px-2.5 text-start hover:bg-surface-container sm:max-w-[15rem]">
              <span className="grid size-6 shrink-0 place-items-center rounded-xs bg-primary-container type-label-small text-on-primary-container">{companyInfo.avatar}</span>
              <span className="hidden min-w-0 flex-1 truncate type-label-large text-foreground min-[390px]:block">{companyInfo.name}</span>
              <ChevronDown className={cn("hidden size-3.5 shrink-0 text-muted-foreground transition-transform min-[390px]:block", companyMenuOpen && "rotate-180")} />
            </button>
            {companyMenuOpen && (
              <div role="menu" className="absolute start-0 top-full z-50 mt-2 w-[min(18rem,calc(100vw-1rem))] rounded-sm border border-border bg-popover p-2 text-popover-foreground shadow-e3">
                <p className="px-2 py-1 type-label-small uppercase text-muted-foreground">{t("common.selectCompany")}</p>
                <button type="button" role="menuitem" onClick={() => setCompanyMenuOpen(false)} className="mt-1 flex min-h-11 w-full items-center justify-between gap-3 rounded-sm bg-primary-container px-3 text-start text-on-primary-container">
                  <span className="min-w-0"><strong className="block truncate type-label-large">{companyInfo.name}</strong><span className="block type-label-small opacity-75">{companyInfo.code}</span></span>
                  <Check className="size-4 shrink-0" />
                </button>
              </div>
            )}
          </div>
        ) : (
          <div className="flex h-10 min-w-0 items-center gap-2 rounded-sm border border-border bg-surface-container-low px-2.5">
            <ContextIcon className="size-4 shrink-0 text-primary" />
            <span className="hidden max-w-44 truncate type-label-large text-foreground min-[375px]:block">{contextLabel}</span>
          </div>
        )}
      </div>

      {showSearch && (
        <div className="mx-2 hidden min-w-0 max-w-xl flex-1 md:block">
          <button type="button" onClick={() => window.dispatchEvent(new KeyboardEvent("keydown", { key: "k", ctrlKey: true, bubbles: true }))} aria-label={`${t("common.openCommandPalette")} (Ctrl+K)`} className="mx-auto flex h-10 w-full max-w-[32rem] items-center gap-2.5 rounded-full border border-border bg-surface-container-low px-4 text-muted-foreground shadow-e0 hover:bg-surface-container hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <Search className="size-4 shrink-0" />
            <span className="min-w-0 flex-1 truncate text-start type-body-small">{t("common.searchPlaceholder")}</span>
            <kbd className="rounded-full border border-border bg-card px-2 py-0.5 type-label-small">Ctrl+K</kbd>
          </button>
        </div>
      )}

      <div className="flex shrink-0 items-center gap-1.5">
        <button type="button" onClick={toggleLanguage} aria-label={t("common.switchLanguage")} title={language === "en" ? t("common.switchToArabic") : t("common.switchToEnglish")} className="hidden h-10 items-center gap-1.5 rounded-sm border border-border px-2.5 type-label-large text-foreground hover:bg-secondary sm:flex">
          <Globe className="size-4 text-primary" /><span>{language === "en" ? "العربية" : "EN"}</span>
        </button>

        <div className="relative hidden sm:block">
          <button type="button" onClick={() => { closeOtherMenus("theme"); setThemeMenuOpen((open) => !open); }} aria-label={t("common.changeTheme")} aria-expanded={themeMenuOpen} className="grid size-10 place-items-center rounded-full border border-border text-foreground hover:bg-secondary">
            {theme === "light" ? <Sun className="size-4" /> : theme === "dark" ? <Moon className="size-4" /> : <Monitor className="size-4" />}
          </button>
          {themeMenuOpen && (
            <div role="menu" className="absolute end-0 top-full z-50 mt-2 w-40 rounded-sm border border-border bg-popover p-1.5 shadow-e3">
              {themeOptions.map(({ key, label, icon: Icon }) => <button key={key} type="button" role="menuitem" onClick={() => { setTheme(key); setThemeMenuOpen(false); }} className={cn("flex min-h-10 w-full items-center gap-2 rounded-sm px-3 text-start type-label-large hover:bg-secondary", theme === key && "bg-primary-container text-on-primary-container")}><Icon className="size-4" /><span className="flex-1">{label}</span>{theme === key && <Check className="size-3.5" />}</button>)}
            </div>
          )}
        </div>

        {showNotifications && <NotificationBell />}

        {user && (
          <div className="relative">
            <button type="button" onClick={() => { closeOtherMenus("user"); setUserMenuOpen((open) => !open); }} aria-label={t("common.userMenu")} aria-expanded={userMenuOpen} className="flex h-10 items-center gap-2 rounded-full border border-border bg-surface-container-low p-1 pe-2 hover:bg-surface-container focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
              <span className="grid size-8 shrink-0 place-items-center rounded-full bg-primary type-label-large font-bold text-primary-foreground">{avatarInitial}</span>
              <span className="hidden max-w-32 truncate type-label-large text-foreground xl:block">{displayName}</span>
              <ChevronDown className={cn("hidden size-3.5 text-muted-foreground sm:block", userMenuOpen && "rotate-180")} />
            </button>
            {userMenuOpen && (
              <div role="menu" className="absolute end-0 top-full z-50 mt-2 w-[min(18rem,calc(100vw-1rem))] rounded-sm border border-border bg-popover p-2 shadow-e3">
                <div className="mb-1 flex items-center gap-3 rounded-sm bg-surface-container-low p-3">
                  <span className="grid size-10 shrink-0 place-items-center rounded-full bg-primary font-bold text-primary-foreground">{avatarInitial}</span>
                  <span className="min-w-0"><strong className="block truncate type-label-large text-foreground">{displayName}</strong><span className="block truncate type-body-small text-muted-foreground">{displayEmail}</span></span>
                </div>

                {!isPlatform && <button type="button" role="menuitem" onClick={() => { setUserMenuOpen(false); navigate(isTenant ? ROUTES.tenant.profile : ROUTES.profile.root); }} className="flex min-h-10 w-full items-center gap-2 rounded-sm px-3 text-start type-label-large hover:bg-secondary"><UserCircle className="size-4 text-muted-foreground" />{isTenant ? t("tenant.navigation.profile") : t("nav.profile")}</button>}
                {portal === "company" && <button type="button" role="menuitem" onClick={() => { setUserMenuOpen(false); navigate(ROUTES.preferences.root); }} className="flex min-h-10 w-full items-center gap-2 rounded-sm px-3 text-start type-label-large hover:bg-secondary"><SlidersHorizontal className="size-4 text-muted-foreground" />{t("nav.preferences")}</button>}

                <button type="button" role="menuitem" onClick={toggleLanguage} className="flex min-h-10 w-full items-center gap-2 rounded-sm px-3 text-start type-label-large hover:bg-secondary"><Globe className="size-4 text-muted-foreground" /><span className="flex-1">{t("common.language")}</span><span className="text-primary">{language === "en" ? "العربية" : "English"}</span></button>
                <div className="my-1 grid grid-cols-3 gap-1 border-y border-border py-2 sm:hidden">
                  {themeOptions.map(({ key, label, icon: Icon }) => <button key={key} type="button" onClick={() => setTheme(key)} aria-label={label} className={cn("grid min-h-11 place-items-center rounded-sm text-muted-foreground hover:bg-secondary", theme === key && "bg-primary-container text-on-primary-container")}><Icon className="size-4" /></button>)}
                </div>
                <div className="my-1 border-t border-border" />
                <button type="button" role="menuitem" onClick={() => { setUserMenuOpen(false); logout(); }} className="flex min-h-10 w-full items-center gap-2 rounded-sm px-3 text-start type-label-large text-destructive hover:bg-destructive/10"><LogOut className="size-4" />{t("nav.logout")}</button>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
}

export default Topbar;
