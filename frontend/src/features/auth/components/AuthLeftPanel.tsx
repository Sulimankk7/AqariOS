/**
 * AuthLeftPanel — Fixed left panel of the authentication layout.
 * Architectural Glass Skyscraper design system (adaptive Light/Dark modes).
 *
 * Light: Daytime skyscraper with warm ivory glass reflections.
 * Dark: Nighttime skyscraper with smoked glass facade and warm illuminated windows.
 */

import { AqariOSLogo } from "@/shared/components/AqariOSLogo";
import { ArchitecturalLineIllustration } from "@/features/auth/components/ArchitecturalLineIllustration";
import { TRANSLATIONS } from "@/features/auth/constants/translations";

interface AuthLeftPanelProps {
  lang: "en" | "ar";
  isDark?: boolean;
}

export function AuthLeftPanel({ lang, isDark = false }: AuthLeftPanelProps) {
  const t = TRANSLATIONS[lang];

  return (
    <div
      className={`hidden lg:flex lg:w-[40%] flex-col justify-between relative overflow-hidden transition-colors duration-350 ${
        isDark
          ? "bg-gradient-to-b from-[#090B0E] via-[#0E1116] to-[#161B22] border-r border-white/10 text-gray-200"
          : "bg-gradient-to-b from-[#FAFAF7] via-[#F2F1EB] to-[#E6E4D9] border-r border-[#E5E7EB] text-[#111827]"
      }`}
      aria-hidden="true"
    >
      {/* Background Architectural Glass Reflection Streak */}
      <div
        className="absolute inset-0 pointer-events-none opacity-40"
        style={{
          background: isDark
            ? "linear-gradient(135deg, rgba(255,255,255,0.03) 0%, transparent 40%, rgba(255,217,138,0.02) 100%)"
            : "linear-gradient(135deg, rgba(255,255,255,0.6) 0%, transparent 50%, rgba(164,172,134,0.1) 100%)",
        }}
      />

      {/* Top Branding Section */}
      <div className="relative z-10 p-12 pb-0">
        <div className="flex items-center gap-3.5 mb-2.5">
          <AqariOSLogo size={36} />
          <span
            className={`text-2xl font-semibold tracking-tight ${
              isDark ? "text-white" : "text-[#333D29]"
            }`}
          >
            AqariOS
          </span>
        </div>
        <div
          className={`w-8 h-[2px] rounded-full mb-3 ml-0.5 transition-colors duration-350 ${
            isDark ? "bg-[#FFD98A]" : "bg-[#A4AC86]"
          }`}
        />
        <p
          className={`text-[12.5px] font-medium tracking-wider uppercase transition-colors duration-350 ${
            isDark ? "text-[#A4AC86]" : "text-[#656D4A]"
          }`}
        >
          {t.platformSubtitle}
        </p>
      </div>

      {/* Architectural Line Illustration */}
      <div className="relative z-10 w-full px-8 flex items-center justify-center flex-1 my-auto">
        <div className="w-full max-w-[420px] opacity-90 pointer-events-none">
          <ArchitecturalLineIllustration isDark={isDark} />
        </div>
      </div>

      {/* Quiet Enterprise Footer */}
      <div className="relative z-10 p-12 pt-0">
        <p
          className={`text-[12px] font-normal tracking-tight transition-colors duration-350 ${
            isDark ? "text-gray-500" : "text-[#9CA3AF]"
          }`}
        >
          {t.copyright}
        </p>
      </div>
    </div>
  );
}
