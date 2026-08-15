/**
 * I18nProvider — Comprehensive Localization, Pluralization & Direction (RTL/LTR) Management System.
 *
 * Supports English ('en' / LTR) and Arabic ('ar' / RTL) with instant language switching,
 * persistent local storage, parameter interpolation, pluralization rules, and locale formatters.
 */

import React, { createContext, useContext, useEffect, useState } from "react";
import { en } from "./translations/en";
import { ar } from "./translations/ar";
import {
  formatCurrency as fmtCurrency,
  formatDate as fmtDate,
  formatTime as fmtTime,
  formatNumber as fmtNumber,
  FormatCurrencyOptions,
} from "./formatters";

export type Language = "en" | "ar";
export type Direction = "ltr" | "rtl";

const translations = { en, ar };

export interface I18nContextValue {
  language: Language;
  direction: Direction;
  setLanguage: (lang: Language) => void;
  t: (path: string, params?: Record<string, any>) => string;
  formatCurrency: (amount: number, options?: FormatCurrencyOptions) => string;
  formatDate: (date: Date | string | number, options?: Intl.DateTimeFormatOptions) => string;
  formatTime: (date: Date | string | number, options?: Intl.DateTimeFormatOptions) => string;
  formatNumber: (num: number, options?: Intl.NumberFormatOptions) => string;
}

const STORAGE_KEY = "aqarios_lang";

const I18nContext = createContext<I18nContextValue | undefined>(undefined);

export interface I18nProviderProps {
  children: React.ReactNode;
  defaultLanguage?: Language;
}

export function I18nProvider({
  children,
  defaultLanguage = "en",
}: I18nProviderProps) {
  const [language, setLanguageState] = useState<Language>(() => {
    if (typeof window === "undefined") return defaultLanguage;
    const stored = localStorage.getItem(STORAGE_KEY) as Language | null;
    return stored || defaultLanguage;
  });

  const direction: Direction = language === "ar" ? "rtl" : "ltr";
  const activeLocale = language === "ar" ? "ar-JO" : "en-US";

  // Sync `dir="rtl"` or `dir="ltr"` and `lang` attribute on root <html> element
  useEffect(() => {
    const root = document.documentElement;
    root.setAttribute("dir", direction);
    root.setAttribute("lang", language);
  }, [language, direction]);

  const setLanguage = (newLang: Language) => {
    localStorage.setItem(STORAGE_KEY, newLang);
    setLanguageState(newLang);
  };

  /**
   * Arabic Pluralization Suffix Resolution.
   * Rules for count = n:
   * n = 0 -> _zero
   * n = 1 -> _one
   * n = 2 -> _two
   * n in 3..10 -> _few
   * n in 11..99 -> _many
   * n >= 100 -> _other
   */
  const getPluralSuffix = (count: number, lang: Language): string => {
    if (lang === "en") {
      return count === 1 ? "_one" : "_other";
    }

    if (count === 0) return "_zero";
    if (count === 1) return "_one";
    if (count === 2) return "_two";
    const mod100 = count % 100;
    if (mod100 >= 3 && mod100 <= 10) return "_few";
    if (mod100 >= 11 && mod100 <= 99) return "_many";
    return "_other";
  };

  /** Dot-notation translation lookup with pluralization and interpolation */
  const t = (path: any, params?: Record<string, any>): string => {
    if (path === null || path === undefined) return "";
    const strPath = String(path);
    let targetPath = strPath;

    // Handle pluralization if count param is provided and not skipping plural fallback
    if (params && typeof params.count === "number" && !params._skipPlural) {
      const suffix = getPluralSuffix(params.count, language);
      targetPath = `${strPath}${suffix}`;
    }

    const dict = translations[language] || translations.en;
    const keys = targetPath.split(".");
    let result: any = dict;

    for (const key of keys) {
      if (result && typeof result === "object" && key in result) {
        result = result[key];
      } else {
        // Fallback to base key or English dictionary
        let fallback: any = translations.en;
        for (const fk of keys) {
          if (fallback && typeof fallback === "object" && fk in fallback) {
            fallback = fallback[fk];
          } else {
            // Check without plural suffix if missing
            if (targetPath !== path) {
              return t(path, { ...params, _skipPlural: true });
            }
            return path;
          }
        }
        result = fallback;
        break;
      }
    }

    if (typeof result !== "string") {
      return path;
    }

    if (params) {
      return Object.entries(params).reduce((acc, [paramKey, paramVal]) => {
        return acc.replace(new RegExp(`\\{\\{${paramKey}\\}\\}|\\{${paramKey}\\}`, "g"), String(paramVal));
      }, result);
    }

    return result;
  };

  const formatCurrency = (amount: number, options: FormatCurrencyOptions = {}) =>
    fmtCurrency(amount, { locale: activeLocale, ...options });

  const formatDate = (date: Date | string | number, options?: Intl.DateTimeFormatOptions) =>
    fmtDate(date, activeLocale, options);

  const formatTime = (date: Date | string | number, options?: Intl.DateTimeFormatOptions) =>
    fmtTime(date, activeLocale, options);

  const formatNumber = (num: number, options?: Intl.NumberFormatOptions) =>
    fmtNumber(num, activeLocale, options);

  return (
    <I18nContext.Provider
      value={{
        language,
        direction,
        setLanguage,
        t,
        formatCurrency,
        formatDate,
        formatTime,
        formatNumber,
      }}
    >
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
