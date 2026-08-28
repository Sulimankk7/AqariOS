/**
 * AuthTopBar — Top navigation bar for the auth right panel.
 * Includes:
 *   - Workflow pill selector (Sign in / Create account)
 *   - Custom SkyscraperThemeToggle switch
 *   - Bilingual English / Arabic language switcher
 */

import { Globe } from "lucide-react";
import { useNavigate, useLocation } from "react-router";
import { ROUTES } from "@/config/routes";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { SkyscraperThemeToggle } from "@/features/auth/components/SkyscraperThemeToggle";

interface AuthTopBarProps {
  lang: "en" | "ar";
  isDark: boolean;
  onLangChange: (lang: "en" | "ar") => void;
  onToggleTheme: () => void;
}

export function AuthTopBar({ lang, isDark, onLangChange, onToggleTheme }: AuthTopBarProps) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const t = TRANSLATIONS[lang];

  const isSignInActive =
    pathname === ROUTES.auth.login ||
    pathname === ROUTES.auth.otp ||
    pathname === ROUTES.auth.verify ||
    pathname === ROUTES.auth.forgotPassword ||
    pathname === ROUTES.auth.resetPassword;

  const isRegisterActive = pathname === ROUTES.auth.register;

  return (
    <div
      className="w-full flex items-center justify-between px-8 py-4 border-b border-outline-variant bg-topbar/95 backdrop-blur-md transition-colors duration-350 shrink-0"
    >
      {/* Quick Workflow Navigation Pill Selector */}
      <div
        className="flex items-center gap-1 p-1 rounded-lg border border-outline-variant bg-surface-container transition-colors duration-350"
      >
        <button
          type="button"
          onClick={() => navigate(ROUTES.auth.login)}
          className={`text-[12.5px] font-medium px-3 py-1.5 rounded-md transition-all duration-150 cursor-pointer ${
            isSignInActive
              ? isDark
                ? "bg-card text-foreground shadow-e1 font-semibold border border-outline-variant"
                : "bg-card text-foreground shadow-e1 font-semibold border border-outline-variant"
              : isDark
              ? "text-muted-foreground hover:text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          {t.signInTab}
        </button>
        <button
          type="button"
          onClick={() => navigate(ROUTES.auth.register)}
          className={`text-[12.5px] font-medium px-3 py-1.5 rounded-md transition-all duration-150 cursor-pointer ${
            isRegisterActive
              ? isDark
                ? "bg-card text-foreground shadow-e1 font-semibold border border-outline-variant"
                : "bg-card text-foreground shadow-e1 font-semibold border border-outline-variant"
              : isDark
              ? "text-muted-foreground hover:text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          {t.createAccountTab}
        </button>
      </div>

      {/* Right Navigation Controls: Skyscraper Theme Toggle + Bilingual Switcher */}
      <div className="flex items-center gap-4">
        {/* Custom Glass Skyscraper Theme Switch */}
        <SkyscraperThemeToggle isDark={isDark} onToggle={onToggleTheme} />

        {/* Bilingual English / Arabic Language Switcher */}
        <div className="flex items-center gap-1.5 text-[13px]">
          <Globe size={14} className="text-muted-foreground" />
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => onLangChange("en")}
              className={`px-1.5 py-0.5 rounded transition-colors cursor-pointer ${
                lang === "en"
                  ? isDark
                    ? "font-semibold text-foreground"
                    : "font-semibold text-foreground"
                  : isDark
                  ? "text-muted-foreground hover:text-foreground"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              English
            </button>
            <span className="text-outline">|</span>
            <button
              type="button"
              onClick={() => onLangChange("ar")}
              className={`px-1.5 py-0.5 rounded transition-colors cursor-pointer ${
                lang === "ar"
                  ? isDark
                    ? "font-semibold text-foreground"
                    : "font-semibold text-foreground"
                  : isDark
                  ? "text-muted-foreground hover:text-foreground"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              العربية
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
