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
      className="w-full px-8 py-4 border-t border-outline-variant bg-topbar/80 flex items-center justify-between text-[12px] font-medium text-muted-foreground transition-colors duration-350 shrink-0"
    >
      <span className="hidden sm:inline">{t.copyright}</span>
      <div className="flex items-center gap-4">
        <a
          href="#"
          className="transition-colors hover:text-foreground"
        >
          {t.privacy}
        </a>
        <span>·</span>
        <a
          href="#"
          className="transition-colors hover:text-foreground"
        >
          {t.terms}
        </a>
        <span>·</span>
        <a
          href="#"
          className="transition-colors hover:text-foreground"
        >
          {t.security}
        </a>
        <span>·</span>
        <span
          className="font-mono font-semibold text-primary"
        >
          {t.version}
        </span>
      </div>
    </div>
  );
}
