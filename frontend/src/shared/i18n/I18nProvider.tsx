/**
 * I18nProvider — Comprehensive Localization, Pluralization & Direction (RTL/LTR) Management System.
 *
 * Supports English ('en' / LTR) and Arabic ('ar' / RTL) with instant language switching,
 * persistent local storage, parameter interpolation, pluralization rules, and locale formatters.
 */

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import {
  formatCurrency as fmtCurrency,
  formatDate as fmtDate,
  formatTime as fmtTime,
  formatNumber as fmtNumber,
  FormatCurrencyOptions,
} from "./formatters";
import {
  DEFAULT_LANGUAGE,
  getDirection,
  getInitialLanguage,
  getLocale,
  LANGUAGE_STORAGE_KEY,
  Language,
  Direction,
  setRuntimeLanguage,
  translate,
  TranslationArgument,
} from "./runtime";

export type { Language, Direction } from "./runtime";

export interface I18nContextValue {
  language: Language;
  direction: Direction;
  setLanguage: (lang: Language) => void;
  t: (path: string, argument?: TranslationArgument) => string;
  formatCurrency: (amount: number, options?: FormatCurrencyOptions) => string;
  formatDate: (date: Date | string | number, options?: Intl.DateTimeFormatOptions) => string;
  formatTime: (date: Date | string | number, options?: Intl.DateTimeFormatOptions) => string;
  formatNumber: (num: number, options?: Intl.NumberFormatOptions) => string;
}

const I18nContext = createContext<I18nContextValue | undefined>(undefined);

export interface I18nProviderProps {
  children: React.ReactNode;
  defaultLanguage?: Language;
}

export function I18nProvider({
  children,
  defaultLanguage = DEFAULT_LANGUAGE,
}: I18nProviderProps) {
  const [language, setLanguageState] = useState<Language>(() => getInitialLanguage(defaultLanguage));
  setRuntimeLanguage(language);

  const direction = getDirection(language);
  const activeLocale = getLocale(language);

  // Sync `dir="rtl"` or `dir="ltr"` and `lang` attribute on root <html> element
  useEffect(() => {
    const root = document.documentElement;
    root.setAttribute("dir", direction);
    root.setAttribute("lang", language);
    setRuntimeLanguage(language);
  }, [language, direction]);

  const setLanguage = useCallback((newLang: Language) => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, newLang);
    setRuntimeLanguage(newLang);
    setLanguageState(newLang);
  }, []);

  const t = useCallback(
    (path: string, argument?: TranslationArgument) => translate(language, path, argument),
    [language],
  );

  const formatCurrency = (amount: number, options: FormatCurrencyOptions = {}) =>
    fmtCurrency(amount, { locale: activeLocale, ...options });

  const formatDate = (date: Date | string | number, options?: Intl.DateTimeFormatOptions) =>
    fmtDate(date, activeLocale, options);

  const formatTime = (date: Date | string | number, options?: Intl.DateTimeFormatOptions) =>
    fmtTime(date, activeLocale, options);

  const formatNumber = (num: number, options?: Intl.NumberFormatOptions) =>
    fmtNumber(num, activeLocale, options);

  const value = useMemo(() => ({
    language,
    direction,
    setLanguage,
    t,
    formatCurrency,
    formatDate,
    formatTime,
    formatNumber,
  }), [language, direction, setLanguage, t]);

  return (
    <I18nContext.Provider value={value}>
      {children}
    </I18nContext.Provider>
  );
}

export function useTranslation(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error("useTranslation must be used within an I18nProvider");
  }
  return context;
}
