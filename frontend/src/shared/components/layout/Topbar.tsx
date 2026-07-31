/**
 * Topbar Component — Enterprise Shell Header.
 *
 * Contains:
 * - Mobile hamburger
 * - Company Switcher dropdown
 * - Global Search trigger (Ctrl+K badge)
 * - Language toggle
 * - Theme selector
 * - Notification bell
 * - User menu (Profile, Preferences, Language, Appearance, Logout)
 *
 * Architecture Rule:
 * Logout calls queryClient.clear() then auth.logout() — never accesses localStorage directly.
 */

import React, { useState } from "react";
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
} from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { useTheme } from "@/shared/theme";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useQueryClient } from "@tanstack/react-query";
import { ROUTES } from "@/config/routes";

export interface TopbarProps {
  onOpenMobileNav: () => void;
}

export function Topbar({ onOpenMobileNav }: TopbarProps) {
  const { t, language, setLanguage } = useTranslation();
  const { theme, setTheme } = useTheme();
  const { user, logout } = useAuth();
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [showCompanyMenu, setShowCompanyMenu] = useState(false);
  const [showThemeMenu, setShowThemeMenu] = useState(false);
  const [showNotificationMenu, setShowNotificationMenu] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);

  const toggleLanguage = () => {
    setLanguage(language === "en" ? "ar" : "en");
  };

  /**
   * Logout: clears TanStack Query cache first, then clears auth state.
   * Redirects to /auth/login.
   */
  const handleLogout = () => {
    queryClient.clear();
    logout();
    setShowUserMenu(false);
    navigate(ROUTES.auth.login, { replace: true });
  };

  /** Close all dropdowns when navigating */
  const handleNavigate = (path: string) => {
    setShowUserMenu(false);
    setShowCompanyMenu(false);
    navigate(path);
  };

  // Company switcher — placeholder until backend provides company data
  const mockCompany = {
    name: user?.name ? `${user.name}'s Organization` : "AqariOS Organization",
    code: "AQ-ORG",
    avatar: user?.name ? user.name[0].toUpperCase() : "A",
  };

  return (
    <header
      className="sticky top-0 z-30 w-full h-14 border-b border-border bg-card/95 backdrop-blur-sm px-4 sm:px-6 flex items-center justify-between gap-4"
      role="banner"
    >
      {/* Left: Mobile Hamburger + Company Switcher */}
      <div className="flex items-center gap-3">
        {/* Mobile Hamburger */}
        <button
          onClick={onOpenMobileNav}
          aria-label="Open navigation menu"
          className="lg:hidden p-2 rounded-lg text-muted-foreground hover:bg-secondary hover:text-foreground cursor-pointer transition-colors"
        >
          <Menu className="w-5 h-5" />
        </button>

        {/* Company Switcher */}
        <div className="relative">
          <button
            onClick={() => setShowCompanyMenu((prev) => !prev)}
            aria-label="Switch company"
            aria-expanded={showCompanyMenu}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-border bg-secondary/80 hover:bg-secondary text-xs font-medium transition-colors cursor-pointer"
          >
            {/* Company avatar */}
            <div className="w-5 h-5 rounded bg-brand-green-900 text-white flex items-center justify-center text-[9px] font-bold shrink-0">
              {mockCompany.avatar}
            </div>
            <div className="hidden sm:block text-start truncate max-w-32">
              <span className="font-semibold text-foreground block truncate">
                {mockCompany.name}
              </span>
              <span className="text-[9px] text-muted-foreground block">
                {mockCompany.code}
              </span>
            </div>
            <ChevronDown
              className={`w-3 h-3 text-muted-foreground shrink-0 transition-transform ${showCompanyMenu ? "rotate-180" : ""}`}
            />
          </button>

          {showCompanyMenu && (
            <div
              className="absolute start-0 top-full mt-2 w-56 rounded-lg border border-border bg-card shadow-lg p-1.5 text-xs z-50"
              role="menu"
            >
              <div className="px-2 py-1 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider mb-1">
                {t("common.selectCompany")}
              </div>
              <button
                onClick={() => setShowCompanyMenu(false)}
                className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-secondary text-start transition-colors cursor-pointer"
                role="menuitem"
              >
                <div>
                  <p className="font-medium text-foreground">{mockCompany.name}</p>
                  <p className="text-[10px] text-muted-foreground">{mockCompany.code}</p>
                </div>
                <Check className="w-3.5 h-3.5 text-primary" />
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Center: Search trigger with Ctrl+K badge */}
      <button
        onClick={() => {
          window.dispatchEvent(
            new KeyboardEvent("keydown", { key: "k", ctrlKey: true, bubbles: true })
          );
        }}
        aria-label="Open command palette (Ctrl+K)"
        className="hidden md:flex items-center gap-2 px-3.5 py-1.5 w-56 lg:w-96 rounded-lg border border-border bg-secondary text-muted-foreground text-xs hover:border-border-strong transition-colors cursor-pointer"
      >
        <Search className="w-3.5 h-3.5 shrink-0" />
        <span className="truncate text-start flex-1">{t("common.searchPlaceholder")}</span>
        <kbd className="hidden lg:inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-mono rounded bg-card border border-border text-muted-foreground">
          Ctrl+K
        </kbd>
      </button>

      {/* Right: Language, Theme, Notifications, User */}
      <div className="flex items-center gap-1.5">
        {/* Language Toggle */}
        <button
          onClick={toggleLanguage}
          aria-label="Switch language"
          className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg border border-border hover:bg-secondary text-foreground text-xs font-medium transition-colors cursor-pointer"
        >
          <Globe className="w-3.5 h-3.5 text-brand-green-600" />
          <span className="hidden sm:inline">{language === "en" ? "العربية" : "English"}</span>
        </button>

        {/* Theme Selector */}
        <div className="relative">
          <button
            onClick={() => setShowThemeMenu((prev) => !prev)}
            aria-label="Change theme"
            aria-expanded={showThemeMenu}
            className="p-2 rounded-lg border border-border hover:bg-secondary text-foreground transition-colors cursor-pointer"
          >
            {theme === "light" && <Sun className="w-3.5 h-3.5 text-amber-500" />}
            {theme === "dark" && <Moon className="w-3.5 h-3.5 text-blue-400" />}
            {theme === "system" && <Monitor className="w-3.5 h-3.5 text-muted-foreground" />}
          </button>

          {showThemeMenu && (
            <div
              className="absolute end-0 top-full mt-2 w-36 rounded-lg border border-border bg-card shadow-lg p-1 space-y-0.5 text-xs z-50"
              role="menu"
            >
              {[
                { key: "light", label: t("theme.light"), icon: <Sun className="w-3.5 h-3.5" /> },
                { key: "dark", label: t("theme.dark"), icon: <Moon className="w-3.5 h-3.5" /> },
                { key: "system", label: t("theme.system"), icon: <Monitor className="w-3.5 h-3.5" /> },
              ].map(({ key, label, icon }) => (
                <button
                  key={key}
                  onClick={() => { setTheme(key as "light" | "dark" | "system"); setShowThemeMenu(false); }}
                  className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-start transition-colors cursor-pointer ${theme === key ? "bg-secondary font-semibold" : "hover:bg-secondary"}`}
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
        <div className="relative">
          <button
            onClick={() => setShowNotificationMenu((prev) => !prev)}
            aria-label="Notifications"
            aria-expanded={showNotificationMenu}
            className="relative p-2 rounded-lg border border-border hover:bg-secondary text-foreground transition-colors cursor-pointer"
          >
            <Bell className="w-3.5 h-3.5 text-brand-brown-600" />
            {/* Unread indicator — will be driven by real API count in future */}
            <span
              className="absolute top-1 end-1 w-1.5 h-1.5 rounded-full bg-danger"
              aria-label="Unread notifications"
            />
          </button>

          {showNotificationMenu && (
            <div
              className="absolute end-0 top-full mt-2 w-72 rounded-lg border border-border bg-card shadow-lg p-2 text-xs z-50"
              role="dialog"
              aria-label="Notifications panel"
            >
              <div className="flex items-center justify-between pb-2 mb-2 border-b border-border">
                <span className="font-semibold text-foreground">{t("common.notifications")}</span>
              </div>
              {/* Empty state */}
              <div className="flex flex-col items-center justify-center py-6 text-muted-foreground gap-2">
                <Bell className="w-8 h-8 opacity-30" />
                <p>{t("common.noNotifications") || "No notifications"}</p>
              </div>
            </div>
          )}
        </div>

        {/* User Menu */}
        {user && (
          <div className="relative">
            <button
              onClick={() => setShowUserMenu((prev) => !prev)}
              aria-label="User menu"
              aria-expanded={showUserMenu}
              className="flex items-center gap-2 p-1.5 rounded-lg border border-border hover:bg-secondary text-xs transition-colors cursor-pointer"
            >
              <div className="w-6 h-6 rounded-full bg-brand-brown-600 text-white flex items-center justify-center text-[10px] font-bold shrink-0">
                {user.name ? user.name[0].toUpperCase() : "U"}
              </div>
              <div className="hidden sm:flex flex-col text-start max-w-24">
                <span className="font-medium text-foreground truncate">{user.name || "User"}</span>
                <span className="text-[9px] text-muted-foreground truncate">{user.email}</span>
              </div>
              <ChevronDown
                className={`w-3 h-3 text-muted-foreground shrink-0 transition-transform ${showUserMenu ? "rotate-180" : ""}`}
              />
            </button>

            {showUserMenu && (
              <div
                className="absolute end-0 top-full mt-2 w-52 rounded-lg border border-border bg-card shadow-lg p-1.5 text-xs z-50 space-y-0.5"
                role="menu"
              >
                {/* User identity header */}
                <div className="px-2.5 py-2 border-b border-border mb-1">
                  <p className="font-semibold text-foreground truncate">{user.name}</p>
                  <p className="text-[10px] text-muted-foreground truncate">{user.email}</p>
                </div>

                {/* Profile */}
                <button
                  onClick={() => handleNavigate(ROUTES.profile.root)}
                  className="w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-start hover:bg-secondary transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <UserCircle className="w-3.5 h-3.5 text-muted-foreground" />
                  <span>{t("nav.profile") || "Profile"}</span>
                </button>

                {/* Preferences */}
                <button
                  onClick={() => handleNavigate(ROUTES.preferences.root)}
                  className="w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-start hover:bg-secondary transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <SlidersHorizontal className="w-3.5 h-3.5 text-muted-foreground" />
                  <span>{t("nav.preferences") || "Preferences"}</span>
                </button>

                {/* Language */}
                <button
                  onClick={() => { toggleLanguage(); setShowUserMenu(false); }}
                  className="w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-start hover:bg-secondary transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <Globe className="w-3.5 h-3.5 text-muted-foreground" />
                  <span>{language === "en" ? "العربية" : "English"}</span>
                </button>

                {/* Appearance */}
                <button
                  onClick={() => { setShowUserMenu(false); setShowThemeMenu(true); }}
                  className="w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-start hover:bg-secondary transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <Palette className="w-3.5 h-3.5 text-muted-foreground" />
                  <span>{t("theme.mode") || "Appearance"}</span>
                </button>

                <div className="border-t border-border my-1" />

                {/* Logout */}
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-start text-danger hover:bg-danger/10 transition-colors cursor-pointer"
                  role="menuitem"
                >
                  <LogOut className="w-3.5 h-3.5" />
                  <span>{t("nav.logout")}</span>
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
}
