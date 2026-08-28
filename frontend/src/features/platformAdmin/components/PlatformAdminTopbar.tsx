import { Globe, LogOut, Menu, ShieldCheck } from "lucide-react";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTranslation } from "@/shared/i18n";

export function PlatformAdminTopbar({ onOpenNavigation }: { onOpenNavigation: () => void }) {
  const { user, logout } = useAuth();
  const { t, language, setLanguage } = useTranslation();
  return <header className="sticky top-0 z-30 flex h-14 items-center justify-between border-b border-border bg-card/95 px-3 backdrop-blur-md sm:px-5">
    <div className="flex min-w-0 items-center gap-2"><button onClick={onOpenNavigation} className="rounded-lg p-2 text-muted-foreground hover:bg-secondary lg:hidden" aria-label={t("platformAdmin.nav.open")}><Menu className="h-5 w-5" /></button><ShieldCheck className="h-4 w-4 text-primary" /><span className="truncate text-sm font-semibold">{t("platformAdmin.title")}</span></div>
    <div className="flex items-center gap-2"><span className="hidden max-w-48 truncate text-xs text-muted-foreground sm:block">{user?.name}</span><button onClick={() => setLanguage(language === "ar" ? "en" : "ar")} className="rounded-lg border border-border px-2.5 py-1.5 text-xs"><Globe className="me-1 inline h-3.5 w-3.5" />{language === "ar" ? "EN" : "العربية"}</button><button onClick={logout} className="rounded-lg p-2 text-muted-foreground hover:bg-secondary hover:text-destructive" aria-label={t("nav.logout")}><LogOut className="h-4 w-4" /></button></div>
  </header>;
}
