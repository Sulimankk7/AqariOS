/**
 * AuthLayout — Master layout container for all authentication routes.
 *
 * Design System: Architectural Glass Skyscraper
 * Features:
 * - Adaptive Light / Dark mode toggle with custom SkyscraperThemeToggle switch.
 * - Glass panels with subtle backdrop-blur and warm architectural window lighting in dark mode.
 * - Exact Framer Motion page slide & fade transitions preserved.
 */

import { useState } from "react";
import { Outlet, useLocation, Navigate } from "react-router";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { motion, AnimatePresence } from "motion/react";
import { CheckCircle2 } from "lucide-react";
import { AuthLeftPanel } from "@/features/auth/components/AuthLeftPanel";
import { AuthTopBar } from "@/features/auth/components/AuthTopBar";
import { AuthFooter } from "@/features/auth/components/AuthFooter";
import { ROUTES } from "@/config/routes";
import { useTheme } from "@/shared/theme";

export function AuthLayout() {
  const location = useLocation();
  const { isAuthenticated, authStatus, user } = useAuth();

  if (authStatus === "authenticated" && user?.roleCode) {
    if (user.roleCode === "TENANT") {
      return <Navigate to="/tenant/dashboard" replace />;
    }
    if (user.roleCode === "COMPANY_ADMIN") {
      return <Navigate to={ROUTES.dashboard.root} replace />;
    }
  }

  // Bilingual system language state (en/ar)
  const [lang, setLang] = useState<"en" | "ar">("en");
  const [feedbackMessage, setFeedbackMessage] = useState<string | null>(null);

  // Use the global ThemeProvider — no direct localStorage access
  const { theme, setTheme } = useTheme();
  const isDark = theme === "dark" || (theme === "system" && typeof window !== "undefined" && window.matchMedia("(prefers-color-scheme: dark)").matches);

  const toggleTheme = () => {
    setTheme(isDark ? "light" : "dark");
  };

  const isWideFormPage = location.pathname === ROUTES.auth.register;

  return (
    <div
      className={`min-h-screen w-full flex text-foreground select-none transition-colors duration-350 ${
        isDark ? "dark bg-[#0E1116] text-[#F0F3F6]" : "bg-[#FAFAF7] text-[#111827]"
      }`}
      style={{ fontFamily: "'Inter', system-ui, sans-serif" }}
    >
      {/* LEFT PANEL — Fixed branding & architectural illustration (40% width) */}
      <AuthLeftPanel lang={lang} isDark={isDark} />

      {/* RIGHT PANEL — Route content container (60% width) */}
      <div
        className={`flex-1 lg:w-[60%] flex flex-col min-h-screen transition-colors duration-350 ${
          isDark ? "bg-[#0E1116]" : "bg-white/80"
        }`}
        dir={lang === "ar" ? "rtl" : "ltr"}
      >
        {/* Fixed Top Bar */}
        <AuthTopBar
          lang={lang}
          isDark={isDark}
          onLangChange={setLang}
          onToggleTheme={toggleTheme}
        />

        {/* Dynamic Form Area with Framer Motion slide & fade transition */}
        <div className="flex-1 flex flex-col justify-center items-center px-6 py-8 overflow-y-auto">
          <div className={`w-full ${isWideFormPage ? "max-w-[540px]" : "max-w-[420px]"}`}>
            {/* Feedback Alert Toast Banner */}
            {feedbackMessage && (
              <motion.div
                initial={{ opacity: 0, y: -8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                className={`mb-6 p-3.5 border rounded-lg flex items-center gap-2.5 text-[13px] font-medium transition-colors duration-350 ${
                  isDark
                    ? "bg-[#333D29]/30 border-[#414833]/50 text-[#FFD98A]"
                    : "bg-[#414833]/10 border-[#414833]/20 text-[#333D29]"
                }`}
              >
                <CheckCircle2 size={16} className={isDark ? "text-[#FFD98A]" : "text-[#414833]"} />
                <span>{feedbackMessage}</span>
              </motion.div>
            )}

            <AnimatePresence mode="wait">
              <motion.div
                key={location.pathname}
                initial={{ opacity: 0, x: lang === "ar" ? -18 : 18 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: lang === "ar" ? 18 : -18 }}
                transition={{ duration: 0.22, ease: [0.16, 1, 0.3, 1] }}
                className="w-full"
              >
                <Outlet context={{ lang, isDark, setFeedbackMessage }} />
              </motion.div>
            </AnimatePresence>
          </div>
        </div>

        {/* Fixed Footer Bar */}
        <AuthFooter lang={lang} isDark={isDark} />
      </div>
    </div>
  );
}
