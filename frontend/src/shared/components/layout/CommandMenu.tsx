/**
 * CommandMenu Component — Global Command Palette (Ctrl+K / ⌘K).
 *
 * Features:
 * - Keyboard shortcut: Ctrl+K or ⌘K to open, Escape to close
 * - Search/filter across all registered application routes
 * - Navigate by clicking or pressing Enter on a result
 * - Theme and language quick actions
 * - Full keyboard navigation with arrow keys
 */

import React, { useEffect, useState, useRef, useMemo } from "react";
import { useNavigate } from "react-router";
import {
  Search,
  LayoutDashboard,
  Building2,
  Home,
  Car,
  FileText,
  Users,
  Wallet,
  Calculator,
  Wrench,
  Store,
  FolderOpen,
  Bell,
  Settings,
  UserCircle,
  SlidersHorizontal,
  Sun,
  Moon,
  Monitor,
  Globe,
  X,
} from "lucide-react";
import { useTranslation } from "@/shared/i18n";
import { useTheme } from "@/shared/theme";
import { ROUTES } from "@/config/routes";

// ── Command item definition ────────────────────────────────────────────────────

interface CommandItem {
  id: string;
  label: string;
  description?: string;
  icon: React.ComponentType<{ className?: string }>;
  group: string;
  action: () => void;
  keywords?: string[];
}

// ── Component ──────────────────────────────────────────────────────────────────

export function CommandMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [selectedIndex, setSelectedIndex] = useState(0);

  const { t, language, setLanguage } = useTranslation();
  const { theme, setTheme } = useTheme();
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);

  // ── Keyboard shortcut handler ────────────────────────────────────────────────

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "k") {
        e.preventDefault();
        setIsOpen((prev) => !prev);
      }
      if (e.key === "Escape") {
        setIsOpen(false);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  // Focus input when opened
  useEffect(() => {
    if (isOpen) {
      setQuery("");
      setSelectedIndex(0);
      setTimeout(() => inputRef.current?.focus(), 50);
    }
  }, [isOpen]);

  const close = () => setIsOpen(false);

  const handleNavigate = (path: string) => {
    navigate(path);
    close();
  };

  // ── All registered command items ─────────────────────────────────────────────

  const allCommands: CommandItem[] = useMemo(() => [
    // Navigation — Dashboard
    {
      id: "dashboard",
      label: t("nav.dashboard"),
      description: "Overview and KPI metrics",
      icon: LayoutDashboard,
      group: "Navigation",
      action: () => handleNavigate(ROUTES.dashboard.root),
      keywords: ["home", "overview", "main"],
    },
    // Property
    {
      id: "buildings",
      label: t("nav.buildings") !== "nav.buildings" ? t("nav.buildings") : "Buildings",
      description: "Manage building portfolio",
      icon: Building2,
      group: "Property",
      action: () => handleNavigate(ROUTES.buildings.root),
      keywords: ["property", "real estate", "assets"],
    },
    {
      id: "apartments",
      label: t("nav.apartments") !== "nav.apartments" ? t("nav.apartments") : "Apartments",
      description: "Manage apartment units",
      icon: Home,
      group: "Property",
      action: () => handleNavigate(ROUTES.apartments.root),
      keywords: ["units", "flats", "residences"],
    },
    {
      id: "parking",
      label: t("nav.parking") !== "nav.parking" ? t("nav.parking") : "Parking",
      description: "Track parking spaces",
      icon: Car,
      group: "Property",
      action: () => handleNavigate(ROUTES.parking.root),
      keywords: ["car", "garage", "spaces"],
    },
    // Leasing
    {
      id: "leases",
      label: t("nav.leases") !== "nav.leases" ? t("nav.leases") : "Leases",
      description: "Manage lease contracts",
      icon: FileText,
      group: "Leasing",
      action: () => handleNavigate(ROUTES.leases.root),
      keywords: ["contracts", "agreements", "rental"],
    },
    {
      id: "tenants",
      label: t("nav.tenants") !== "nav.tenants" ? t("nav.tenants") : "Tenants",
      description: "Tenant profiles and history",
      icon: Users,
      group: "Leasing",
      action: () => handleNavigate(ROUTES.tenants.root),
      keywords: ["renters", "occupants", "residents"],
    },
    // Finance
    {
      id: "payments",
      label: t("nav.payments") !== "nav.payments" ? t("nav.payments") : "Payments",
      description: "Rent collection and invoicing",
      icon: Wallet,
      group: "Finance",
      action: () => handleNavigate(ROUTES.payments.root),
      keywords: ["rent", "invoices", "collections", "money"],
    },
    {
      id: "financial-operations",
      label: t("nav.financialOps") !== "nav.financialOps" ? t("nav.financialOps") : "Financial Operations",
      description: "Accounting and reporting",
      icon: Calculator,
      group: "Finance",
      action: () => handleNavigate(ROUTES.financialOperations.root),
      keywords: ["accounting", "expenses", "reports", "financials"],
    },
    // Operations
    {
      id: "maintenance",
      label: t("nav.maintenance") !== "nav.maintenance" ? t("nav.maintenance") : "Maintenance",
      description: "Work orders and facilities",
      icon: Wrench,
      group: "Operations",
      action: () => handleNavigate(ROUTES.maintenance.root),
      keywords: ["repairs", "work orders", "facilities"],
    },
    {
      id: "marketplace",
      label: t("nav.marketplace") !== "nav.marketplace" ? t("nav.marketplace") : "Marketplace",
      description: "Integrations and add-ons",
      icon: Store,
      group: "Operations",
      action: () => handleNavigate(ROUTES.marketplace.root),
      keywords: ["store", "apps", "integrations"],
    },
    {
      id: "documents",
      label: t("nav.documents") !== "nav.documents" ? t("nav.documents") : "Documents",
      description: "Document repository",
      icon: FolderOpen,
      group: "Operations",
      action: () => handleNavigate(ROUTES.documents.root),
      keywords: ["files", "contracts", "repository"],
    },
    // System
    {
      id: "notifications",
      label: t("nav.notifications") !== "nav.notifications" ? t("nav.notifications") : "Notifications",
      description: "Alerts and system messages",
      icon: Bell,
      group: "System",
      action: () => handleNavigate(ROUTES.notifications.root),
      keywords: ["alerts", "messages"],
    },
    {
      id: "settings",
      label: t("nav.settings") !== "nav.settings" ? t("nav.settings") : "Settings",
      description: "System configuration",
      icon: Settings,
      group: "System",
      action: () => handleNavigate(ROUTES.settings.root),
      keywords: ["config", "system", "admin"],
    },
    {
      id: "profile",
      label: t("nav.profile") !== "nav.profile" ? t("nav.profile") : "Profile",
      description: "Your account information",
      icon: UserCircle,
      group: "System",
      action: () => handleNavigate(ROUTES.profile.root),
      keywords: ["account", "me", "user"],
    },
    {
      id: "preferences",
      label: t("nav.preferences") !== "nav.preferences" ? t("nav.preferences") : "Preferences",
      description: "Customize your workspace",
      icon: SlidersHorizontal,
      group: "System",
      action: () => handleNavigate(ROUTES.preferences.root),
      keywords: ["customize", "options", "settings"],
    },
    // Quick actions — Theme
    {
      id: "theme-light",
      label: t("theme.light"),
      description: "Switch to light mode",
      icon: Sun,
      group: "Appearance",
      action: () => { setTheme("light"); close(); },
      keywords: ["light", "bright"],
    },
    {
      id: "theme-dark",
      label: t("theme.dark"),
      description: "Switch to dark mode",
      icon: Moon,
      group: "Appearance",
      action: () => { setTheme("dark"); close(); },
      keywords: ["dark", "night"],
    },
    {
      id: "theme-system",
      label: t("theme.system"),
      description: "Follow system preference",
      icon: Monitor,
      group: "Appearance",
      action: () => { setTheme("system"); close(); },
      keywords: ["auto", "os"],
    },
    // Language
    {
      id: "lang-en",
      label: "English (LTR)",
      description: "Switch to English",
      icon: Globe,
      group: "Language",
      action: () => { setLanguage("en"); close(); },
      keywords: ["english", "ltr"],
    },
    {
      id: "lang-ar",
      label: "العربية (RTL)",
      description: "Switch to Arabic",
      icon: Globe,
      group: "Language",
      action: () => { setLanguage("ar"); close(); },
      keywords: ["arabic", "rtl", "عربي"],
    },
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t, theme, language]);

  // ── Filtering ────────────────────────────────────────────────────────────────

  const filteredCommands = useMemo(() => {
    if (!query.trim()) return allCommands;
    const q = query.toLowerCase();
    return allCommands.filter(
      (cmd) =>
        cmd.label.toLowerCase().includes(q) ||
        cmd.description?.toLowerCase().includes(q) ||
        cmd.group.toLowerCase().includes(q) ||
        cmd.keywords?.some((k) => k.toLowerCase().includes(q))
    );
  }, [allCommands, query]);

  // Group filtered results
  const groupedCommands = useMemo(() => {
    const groups: Record<string, CommandItem[]> = {};
    filteredCommands.forEach((cmd) => {
      if (!groups[cmd.group]) groups[cmd.group] = [];
      groups[cmd.group].push(cmd);
    });
    return groups;
  }, [filteredCommands]);

  // ── Arrow key navigation ─────────────────────────────────────────────────────

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setSelectedIndex((prev) => Math.min(prev + 1, filteredCommands.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setSelectedIndex((prev) => Math.max(prev - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      filteredCommands[selectedIndex]?.action();
    }
  };

  // Reset selection when query changes
  useEffect(() => { setSelectedIndex(0); }, [query]);

  if (!isOpen) return null;

  let flatIndex = 0;

  return (
    <div
      className="fixed inset-0 z-[100] bg-black/60 backdrop-blur-sm flex items-start justify-center pt-16 sm:pt-24 px-4 animate-in fade-in duration-150"
      onClick={close}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
        className="w-full max-w-lg bg-card border border-border rounded-xl shadow-2xl overflow-hidden"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        {/* Search input */}
        <div className="flex items-center gap-3 px-4 py-3 border-b border-border">
          <Search className="w-4 h-4 text-muted-foreground shrink-0" aria-hidden />
          <input
            ref={inputRef}
            type="text"
            placeholder={t("common.searchPlaceholder")}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-full bg-transparent text-sm font-medium focus:outline-none placeholder:text-muted-foreground"
            aria-label="Search commands"
          />
          <button
            onClick={close}
            aria-label="Close command palette"
            className="p-1 rounded-md text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Results */}
        <div className="max-h-96 overflow-y-auto overscroll-contain p-2 space-y-1 text-sm" role="listbox">
          {filteredCommands.length === 0 && (
            <div className="py-8 text-center text-muted-foreground text-xs">
              No results for "{query}"
            </div>
          )}

          {Object.entries(groupedCommands).map(([group, items]) => (
            <div key={group}>
              <div className="px-3 py-1 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
                {group}
              </div>
              {items.map((cmd) => {
                const Icon = cmd.icon;
                const itemIndex = flatIndex++;
                const isSelected = itemIndex === selectedIndex;

                return (
                  <button
                    key={cmd.id}
                    onClick={cmd.action}
                    role="option"
                    aria-selected={isSelected}
                    className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-start transition-colors cursor-pointer ${
                      isSelected
                        ? "bg-secondary text-foreground"
                        : "hover:bg-secondary/70 text-muted-foreground hover:text-foreground"
                    }`}
                  >
                    <div className="w-7 h-7 rounded-md bg-secondary flex items-center justify-center shrink-0">
                      <Icon className="w-3.5 h-3.5" />
                    </div>
                    <div className="min-w-0">
                      <p className="font-medium text-foreground text-xs truncate">{cmd.label}</p>
                      {cmd.description && (
                        <p className="text-[10px] text-muted-foreground truncate">{cmd.description}</p>
                      )}
                    </div>
                    {isSelected && (
                      <kbd className="ms-auto shrink-0 px-1.5 py-0.5 text-[9px] font-mono rounded bg-card border border-border text-muted-foreground">
                        ↵
                      </kbd>
                    )}
                  </button>
                );
              })}
            </div>
          ))}
        </div>

        {/* Footer hint */}
        <div className="px-4 py-2 border-t border-border text-[10px] text-muted-foreground flex items-center gap-3">
          <span><kbd className="font-mono">↑↓</kbd> navigate</span>
          <span><kbd className="font-mono">↵</kbd> select</span>
          <span><kbd className="font-mono">Esc</kbd> close</span>
        </div>
      </div>
    </div>
  );
}
