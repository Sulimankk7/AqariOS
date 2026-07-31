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
      className={`w-full flex items-center justify-between px-8 py-4 border-b transition-colors duration-350 shrink-0 ${
        isDark ? "bg-[#161B22]/60 border-white/10 backdrop-blur-md" : "bg-white/70 border-[#F3F4F6] backdrop-blur-md"
      }`}
    >
      {/* Quick Workflow Navigation Pill Selector */}
      <div
        className={`flex items-center gap-1 p-1 rounded-lg border transition-colors duration-350 ${
          isDark ? "bg-[#0E1116] border-white/10" : "bg-[#F9FAFB] border-[#E5E7EB]"
        }`}
      >
        <button
          type="button"
          onClick={() => navigate(ROUTES.auth.login)}
          className={`text-[12.5px] font-medium px-3 py-1.5 rounded-md transition-all duration-150 cursor-pointer ${
            isSignInActive
              ? isDark
                ? "bg-[#1C2128] text-white shadow-xs font-semibold border border-white/10"
                : "bg-white text-[#333D29] shadow-xs font-semibold"
              : isDark
              ? "text-gray-400 hover:text-gray-200"
              : "text-[#6B7280] hover:text-[#374151]"
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
                ? "bg-[#1C2128] text-white shadow-xs font-semibold border border-white/10"
                : "bg-white text-[#333D29] shadow-xs font-semibold"
              : isDark
              ? "text-gray-400 hover:text-gray-200"
              : "text-[#6B7280] hover:text-[#374151]"
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
          <Globe size={14} className={isDark ? "text-gray-400" : "text-[#9CA3AF]"} />
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => onLangChange("en")}
              className={`px-1.5 py-0.5 rounded transition-colors cursor-pointer ${
                lang === "en"
                  ? isDark
                    ? "font-semibold text-white"
                    : "font-semibold text-[#333D29]"
                  : isDark
                  ? "text-gray-400 hover:text-gray-200"
                  : "text-[#9CA3AF] hover:text-[#4B5563]"
              }`}
            >
              English
            </button>
            <span className={isDark ? "text-gray-600" : "text-[#D1D5DB]"}>|</span>
            <button
              type="button"
              onClick={() => onLangChange("ar")}
              className={`px-1.5 py-0.5 rounded transition-colors cursor-pointer ${
                lang === "ar"
                  ? isDark
                    ? "font-semibold text-white"
                    : "font-semibold text-[#333D29]"
                  : isDark
                  ? "text-gray-400 hover:text-gray-200"
                  : "text-[#9CA3AF] hover:text-[#4B5563]"
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
