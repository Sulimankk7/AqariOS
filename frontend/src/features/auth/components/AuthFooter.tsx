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
      className="flex w-full min-w-0 flex-wrap items-center justify-between gap-3 border-t border-outline-variant bg-topbar/80 px-4 py-3 text-[12px] font-medium text-muted-foreground transition-colors duration-350 sm:px-8 sm:py-4 shrink-0"
    >
      <span className="hidden sm:inline">{t.copyright}</span>
      <div className="flex min-w-0 flex-wrap items-center gap-x-4 gap-y-1">
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
