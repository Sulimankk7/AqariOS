/**
 * AuthLoadingScreen Component.
 * Full-screen clean loading screen displayed during startup authentication initialization.
 * Prevents UI flickering and protects authenticated routes.
 */

import React from "react";
import { useTranslation } from "@/shared/i18n";
import { AqariOSLogo } from "@/shared/components/AqariOSLogo";

export function AuthLoadingScreen() {
  const { t } = useTranslation();
  return (
    <div 
      className="fixed inset-0 z-50 bg-background text-foreground flex flex-col items-center justify-center gap-4 select-none"
      role="status"
      aria-label={t("common.validatingSession")}
    >
      <div className="flex items-center gap-3">
        <AqariOSLogo size={36} />
        <span className="font-bold tracking-tight text-lg">AqariOS</span>
      </div>

      <div className="flex items-center gap-2 text-xs text-muted-foreground font-medium">
        <div className="w-4 h-4 border-2 border-primary border-t-transparent rounded-full animate-spin" />
        <span>{t("common.validatingSession")}</span>
      </div>
    </div>
  );
}
