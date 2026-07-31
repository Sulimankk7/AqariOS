/**
 * AuthFooter — Bottom footer bar of the auth right panel.
 * Adaptive light/dark glass panel aesthetic.
 */

import { TRANSLATIONS } from "@/features/auth/constants/translations";

interface AuthFooterProps {
  lang: "en" | "ar";
  isDark?: boolean;
}

export function AuthFooter({ lang, isDark = false }: AuthFooterProps) {
  const t = TRANSLATIONS[lang];

  return (
    <div
      className={`w-full px-8 py-4 border-t flex items-center justify-between text-[12px] transition-colors duration-350 shrink-0 ${
        isDark
          ? "bg-[#161B22]/40 border-white/10 text-gray-400"
          : "bg-white/40 border-[#F3F4F6] text-[#9CA3AF]"
      }`}
    >
      <span className="hidden sm:inline">{t.copyright}</span>
      <div className="flex items-center gap-4">
        <a
          href="#"
          className={`transition-colors ${
            isDark ? "hover:text-gray-200" : "hover:text-[#4B5563]"
          }`}
        >
          {t.privacy}
        </a>
        <span>·</span>
        <a
          href="#"
          className={`transition-colors ${
            isDark ? "hover:text-gray-200" : "hover:text-[#4B5563]"
          }`}
        >
          {t.terms}
        </a>
        <span>·</span>
        <a
          href="#"
          className={`transition-colors ${
            isDark ? "hover:text-gray-200" : "hover:text-[#4B5563]"
          }`}
        >
          {t.security}
        </a>
        <span>·</span>
        <span
          className={`font-mono font-medium ${
            isDark ? "text-[#A4AC86]" : "text-[#656D4A]"
          }`}
        >
          {t.version}
        </span>
      </div>
    </div>
  );
}
