/**
 * Topbar Component — Enterprise Shell Header.
 *
 * Designed for exceptional UX, visual clarity, and full RTL/LTR responsiveness.
 *
 * Contains:
 * - Mobile hamburger menu trigger
 * - Company/Organization Switcher (Sleek dropdown badge)
 * - Global Search trigger with Ctrl+K shortcut badge
 * - Quick Action Tools (Language Switcher, Theme Selector, Notification Bell)
 * - User Profile & Identity Menu
 */

import React, { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router";
import {
  Menu,
  Search,
  Sun,
  Moon,
  Monitor,
  Globe,
  Bell,
  Building2,
  ChevronDown,
  LogOut,
  UserCircle,
  SlidersHorizontal,
  Check,
  Palette,
  Sparkles,
  ShieldCheck,
} from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { useTheme } from "@/shared/theme";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ROUTES } from "@/config/routes";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";
import { useTenantProfile } from "@/features/tenantPortal/hooks/useTenantProfile";

export interface TopbarProps {
  onOpenMobileNav: () => void;
}

export function Topbar({ onOpenMobileNav }: TopbarProps) {
  const { t, language, setLanguage } = useTranslation();
  const { theme, setTheme } = useTheme();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const isTenant = user?.roleCode === "TENANT";
  const { data: tenantProfile } = useTenantProfile();

  // Role-aware identity display: Tenants use authoritative Tenant profile; Landlords use authenticated User identity
  const displayName = isTenant && tenantProfile?.name ? tenantProfile.name : (user?.name ?? "");
  const displayEmail = isTenant && tenantProfile?.email ? tenantProfile.email : (user?.email ?? "");
  const avatarInitial = displayName ? displayName[0].toUpperCase() : (user?.name ? user.name[0].toUpperCase() : "U");

  const [showCompanyMenu, setShowCompanyMenu] = useState(false);
  const [showThemeMenu, setShowThemeMenu] = useState(false);
  const [showNotificationMenu, setShowNotificationMenu] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);

  const headerRef = useRef<HTMLHeadingElement>(null);

  // Close all dropdowns on click outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (headerRef.current && !headerRef.current.contains(event.target as Node)) {
        setShowCompanyMenu(false);
        setShowThemeMenu(false);
        setShowNotificationMenu(false);
        setShowUserMenu(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const toggleLanguage = () => {
    setLanguage(language === "en" ? "ar" : "en");
  };

  const handleLogout = () => {
    setShowUserMenu(false);
    logout();
  };

  const handleNavigate = (path: string) => {
    setShowUserMenu(false);
    setShowCompanyMenu(false);
    navigate(path);
  };

  // Company details derived from session (for Landlord)
  const companyInfo = user && !isTenant
    ? {
        name: user.companyName || `${user.name}'s Organization`,
        code: "AQ-ORG",
        avatar: user.companyName ? user.companyName[0].toUpperCase() : user.name ? user.name[0].toUpperCase() : "A",
      }
    : null;

  return (
    <header
      ref={headerRef}
      className="sticky top-0 z-30 flex h-16 w-full items-center justify-between gap-3 border-b border-border-strong bg-topbar/95 px-3 backdrop-blur-md sm:px-5"
      role="banner"
    >
      {/* ── Start (Left in LTR / Right in RTL): Mobile Hamburger + Context Indicator ── */}
      <div className="flex items-center gap-2">
        {/* Mobile Hamburger */}
        <button
          onClick={onOpenMobileNav}
          aria-label="Open navigation menu"
          className="lg:hidden p-2 rounded-lg text-muted-foreground hover:bg-secondary hover:text-foreground cursor-pointer transition-colors"
        >
          <Menu className="w-5 h-5" />
        </button>

        {/* Landlord: Company / Organization Switcher */}
        {!isTenant && user && companyInfo && (
          <div className="relative">
            <button
              onClick={() => {
                setShowCompanyMenu((prev) => !prev);
                setShowThemeMenu(false);
                setShowNotificationMenu(false);
                setShowUserMenu(false);
              }}
              aria-label="Switch organization"
              aria-expanded={showCompanyMenu}
              className="flex items-center gap-2 px-2.5 py-1.5 rounded-lg border border-border/70 bg-secondary/60 hover:bg-secondary hover:border-border text-xs transition-all cursor-pointer group"
              title={companyInfo.name}
            >
              <div className="w-5 h-5 rounded bg-primary/15 text-primary border border-primary/30 flex items-center justify-center text-[10px] font-bold shrink-0 group-hover:scale-105 transition-transform">
                {companyInfo.avatar}
              </div>
              <span className="hidden md:inline-block font-medium text-foreground truncate max-w-[150px] text-xs">
                {companyInfo.name}
              </span>
              <ChevronDown
                className={`w-3.5 h-3.5 text-muted-foreground shrink-0 transition-transform duration-200 ${
                  showCompanyMenu ? "rotate-180" : ""
                }`}
              />
            </button>

            {showCompanyMenu && (
              <div
                className="absolute start-0 top-full mt-1.5 w-60 rounded-xl border border-border bg-card shadow-xl p-1.5 text-xs z-50 animate-in fade-in-50 zoom-in-95 duration-100"
                role="menu"
              >
                <div className="px-2.5 py-1 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider mb-1">
                  {t("common.selectCompany")}
                </div>
                <button
                  onClick={() => setShowCompanyMenu(false)}
                  className="w-full flex items-center justify-between px-2.5 py-2 rounded-lg bg-primary/10 border border-primary/20 text-start cursor-pointer"
                  role="menuitem"
                >
                  <div className="flex items-center gap-2 overflow-hidden">
                    <Building2 className="w-4 h-4 text-primary shrink-0" />
                    <div className="truncate">
                      <p className="font-semibold text-foreground truncate">{companyInfo.name}</p>
                      <p className="text-[10px] text-muted-foreground">{companyInfo.code}</p>
                    </div>
                  </div>
                  <Check className="w-4 h-4 text-primary shrink-0" />
                </button>
              </div>
            )}
          </div>
        )}

        {/* Tenant: Portal Identity Badge */}
        {isTenant && (
          <div className="flex items-center gap-2 px-2.5 py-1 rounded-md border border-border bg-secondary/50 text-foreground text-xs font-semibold select-none">
            <ShieldCheck className="w-3.5 h-3.5 text-primary shrink-0" />
            <span className="truncate">{t("tenant.portal", "Tenant Portal")}</span>
          </div>
        )}
      </div>

      {/* ── Center: Global Command Search Trigger (Landlord Only) ── */}
      {!isTenant && (
        <div className="flex-1 max-w-xl mx-2 flex justify-center">
          <button
            onClick={() => {
              window.dispatchEvent(
                new KeyboardEvent("keydown", { key: "k", ctrlKey: true, bubbles: true })
              );
            }}
            aria-label="Open command palette (Ctrl+K)"
            className="w-full max-w-[480px] flex items-center gap-2.5 px-3.5 py-1.5 rounded-full border border-border/80 bg-secondary/50 hover:bg-secondary hover:border-border text-muted-foreground text-xs transition-all cursor-pointer shadow-2xs group"
          >
            <Search className="w-3.5 h-3.5 shrink-0 text-muted-foreground group-hover:text-primary transition-colors" />
            <span className="truncate text-start flex-1 text-muted-foreground group-hover:text-foreground transition-colors">
              {t("common.searchPlaceholder")}
            </span>
            <kbd className="hidden sm:inline-flex items-center gap-0.5 px-2 py-0.5 text-[10px] font-mono rounded-full bg-card border border-border text-muted-foreground shadow-2xs">
              Ctrl+K
            </kbd>
          </button>
        </div>
      )}

      {/* ── End (Right in LTR / Left in RTL): Language, Theme, Notifications, User Profile ── */}
      <div className="flex items-center gap-1.5 shrink-0">
        {/* Language Switcher */}
        <button
          onClick={toggleLanguage}
          aria-label="Switch language"
          title={language === "en" ? "التحويل إلى العربية" : "Switch to English"}
          className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg border border-border/60 hover:bg-secondary text-foreground text-xs font-medium transition-all cursor-pointer"
        >
          <Globe className="w-3.5 h-3.5 text-primary shrink-0" />
          <span className="font-medium text-xs">{language === "en" ? "العربية" : "EN"}</span>
        </button>

        {/* Theme Selector */}
        <div className="relative">
          <button
            onClick={() => {
              setShowThemeMenu((prev) => !prev);
              setShowCompanyMenu(false);
              setShowNotificationMenu(false);
              setShowUserMenu(false);
            }}
            aria-label="Change theme"
            aria-expanded={showThemeMenu}
            className="w-9 h-9 flex items-center justify-center rounded-lg border border-border/60 hover:bg-secondary text-foreground transition-all cursor-pointer"
            title="Appearance Theme"
          >
            {theme === "light" && <Sun className="w-4 h-4 text-amber-500" />}
            {theme === "dark" && <Moon className="w-4 h-4 text-blue-400" />}
            {theme === "system" && <Monitor className="w-4 h-4 text-muted-foreground" />}
          </button>

          {showThemeMenu && (
            <div
              className="absolute end-0 top-full mt-1.5 w-36 rounded-xl border border-border bg-card shadow-xl p-1 space-y-0.5 text-xs z-50 animate-in fade-in-50 zoom-in-95 duration-100"
              role="menu"
            >
              {[
                { key: "light", label: t("theme.light"), icon: <Sun className="w-3.5 h-3.5 text-amber-500" /> },
                { key: "dark", label: t("theme.dark"), icon: <Moon className="w-3.5 h-3.5 text-blue-400" /> },
                { key: "system", label: t("theme.system"), icon: <Monitor className="w-3.5 h-3.5 text-muted-foreground" /> },
              ].map(({ key, label, icon }) => (
                <button
                  key={key}
                  onClick={() => {
                    setTheme(key as "light" | "dark" | "system");
                    setShowThemeMenu(false);
                  }}
                  className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-start transition-colors cursor-pointer ${
                    theme === key ? "bg-secondary font-semibold text-foreground" : "hover:bg-secondary text-muted-foreground hover:text-foreground"
                  }`}
                  role="menuitem"
                >
                  {icon}
                  <span>{label}</span>
                  {theme === key && <Check className="w-3 h-3 ms-auto text-primary" />}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Notification Bell */}
        <NotificationBell />

        {/* Separator */}
        <div className="h-5 w-[1px] bg-border mx-1 hidden sm:block" />

        {/* User Profile Menu */}
        {user && (
          <div className="relative">
            <button
              onClick={() => {
                setShowUserMenu((prev) => !prev);
                setShowCompanyMenu(false);
                setShowThemeMenu(false);
                setShowNotificationMenu(false);
              }}
              aria-label="User menu"
              aria-expanded={showUserMenu}
              className="flex items-center gap-2 p-1 rounded-full border border-border/80 hover:border-primary/40 hover:bg-secondary/60 text-xs transition-all cursor-pointer group"
            >
              {/* User Avatar */}
              <div className="w-7 h-7 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-xs font-bold shrink-0 shadow-xs group-hover:scale-105 transition-transform">
                {avatarInitial}
              </div>

              {/* User Name (Smooth display on desktop) */}
              <span className="hidden md:inline-block font-medium text-foreground text-xs pe-1 truncate max-w-[130px]">
                {displayName}
              </span>
              <ChevronDown
                className={`w-3.5 h-3.5 text-muted-foreground me-1 shrink-0 transition-transform duration-200 ${
                  showUserMenu ? "rotate-180" : ""
                }`}
              />
            </button>

            {showUserMenu && (
              <div
                className="absolute end-0 top-full mt-1.5 w-60 rounded-xl border border-border bg-card shadow-xl p-1.5 text-xs z-50 space-y-1 animate-in fade-in-50 zoom-in-95 duration-100"
                role="menu"
              >
                {/* User identity header card */}
                <div className="p-2.5 rounded-lg bg-secondary/50 border border-border/50 mb-1">
                  <div className="flex items-center gap-2.5">
                    <div className="w-9 h-9 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-sm font-bold shrink-0">
                      {avatarInitial}
                    </div>
                    <div className="truncate">
                      <p className="font-semibold text-foreground truncate">{displayName}</p>
                      <p className="text-[11px] text-muted-foreground truncate">{displayEmail}</p>
                    </div>
                  </div>
                </div>

                {/* Profile */}
                <button
                  onClick={() => handleNavigate(isTenant ? ROUTES.tenant.profile : ROUTES.profile.root)}
                  className="w-full flex items-center gap-2 px-2.5 py-2 rounded-lg text-start hover:bg-secondary text-foreground transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <UserCircle className="w-4 h-4 text-muted-foreground" />
                  <span>{isTenant ? (t("tenant.navigation.profile") || "My Profile") : (t("nav.profile") || "My Profile")}</span>
                </button>

                {/* Preferences (Landlord only) */}
                {!isTenant && (
                  <button
                    onClick={() => handleNavigate(ROUTES.preferences.root)}
                    className="w-full flex items-center gap-2 px-2.5 py-2 rounded-lg text-start hover:bg-secondary text-foreground transition-colors cursor-pointer"
                    role="menuitem"
                  >
                    <SlidersHorizontal className="w-4 h-4 text-muted-foreground" />
                    <span>{t("nav.preferences") || "Preferences"}</span>
                  </button>
                )}

                {/* Quick Language Toggle */}
                <button
                  onClick={() => {
                    toggleLanguage();
                    setShowUserMenu(false);
                  }}
                  className="w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-start hover:bg-secondary text-foreground transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <div className="flex items-center gap-2">
                    <Globe className="w-4 h-4 text-muted-foreground" />
                    <span>{t("common.language") || "Language"}</span>
                  </div>
                  <span className="text-[11px] font-semibold text-primary">
                    {language === "en" ? "العربية" : "English"}
                  </span>
                </button>

                <div className="border-t border-border my-1" />

                {/* Logout */}
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-2 px-2.5 py-2 rounded-lg text-start text-destructive hover:bg-destructive/10 transition-colors cursor-pointer font-medium"
                  role="menuitem"
                >
                  <LogOut className="w-4 h-4" />
                  <span>{t("nav.logout") || "Sign out"}</span>
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
}

export default Topbar;
