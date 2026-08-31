import * as React from "react";
import { cn } from "@/shared/ui/utils";
import { useTranslation } from "@/shared/i18n";

export interface AppShellProps {
  navigation: React.ReactNode;
  topbar: React.ReactNode;
  children: React.ReactNode;
  beforeContent?: React.ReactNode;
  skipLabel?: string;
  className?: string;
  contentClassName?: string;
}

/** Shared authenticated workspace geometry. Navigation and topbar stay role-specific. */
export function AppShell({ navigation, topbar, children, beforeContent, skipLabel, className, contentClassName }: AppShellProps) {
  const { t } = useTranslation();
  return (
    <div data-slot="app-shell" className={cn("flex min-h-screen overflow-x-hidden bg-background text-foreground", className)}>
      {navigation}
      <div className="flex h-screen min-w-0 flex-1 flex-col overflow-y-auto">
        {topbar}
        <main id="main-content" className={cn("mx-auto w-full max-w-7xl flex-1 space-y-4 p-4 sm:p-6 lg:p-8", contentClassName)}>
          <a href="#main-content" className="sr-only focus:not-sr-only focus:fixed focus:start-4 focus:top-4 focus:z-[100] focus:rounded-full focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground focus:shadow-e2">{skipLabel ?? t("common.skipToContent")}</a>
          {beforeContent}
          {children}
        </main>
      </div>
    </div>
  );
}
