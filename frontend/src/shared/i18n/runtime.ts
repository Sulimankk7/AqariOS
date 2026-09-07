import { ar } from "./translations/ar";
import { en } from "./translations/en";

export type Language = "en" | "ar";
export type Direction = "ltr" | "rtl";
export type TranslationParams = Record<string, unknown>;
export type TranslationArgument = TranslationParams | string;

export const DEFAULT_LANGUAGE: Language = "ar";
export const LANGUAGE_STORAGE_KEY = "aqarios_lang";

const dictionaries = { ar, en } as const;
const reportedMissingKeys = new Set<string>();
let activeLanguage: Language = DEFAULT_LANGUAGE;

export function isLanguage(value: unknown): value is Language {
  return value === "ar" || value === "en";
}

export function getInitialLanguage(fallback: Language = DEFAULT_LANGUAGE): Language {
  if (typeof window === "undefined") return fallback;
  const stored = window.localStorage.getItem(LANGUAGE_STORAGE_KEY);
  return isLanguage(stored) ? stored : fallback;
}

export function setRuntimeLanguage(language: Language): void {
  activeLanguage = language;
}

export function getRuntimeLanguage(): Language {
  return activeLanguage;
}

export function getDirection(language: Language): Direction {
  return language === "ar" ? "rtl" : "ltr";
}

export function getLocale(language: Language): string {
  return language === "ar" ? "ar-JO" : "en-US";
}

function lookup(dictionary: unknown, path: string): string | undefined {
  let value: unknown = dictionary;
  for (const segment of path.split(".")) {
    if (!value || typeof value !== "object" || !(segment in value)) return undefined;
    value = (value as Record<string, unknown>)[segment];
  }
  return typeof value === "string" ? value : undefined;
}

function pluralSuffix(count: number, language: Language): string {
  if (language === "en") return count === 1 ? "_one" : "_other";
  if (count === 0) return "_zero";
  if (count === 1) return "_one";
  if (count === 2) return "_two";
  const mod100 = Math.abs(count) % 100;
  if (mod100 >= 3 && mod100 <= 10) return "_few";
  if (mod100 >= 11 && mod100 <= 99) return "_many";
  return "_other";
}

export function reportMissingTranslation(
  path: string,
  language: Language,
  context: { source?: string; kind?: "shared" | "legacy"; dynamic?: boolean } = {},
): void {
  const fingerprint = `${context.kind ?? "shared"}:${language}:${path}`;
  if (reportedMissingKeys.has(fingerprint)) return;
  reportedMissingKeys.add(fingerprint);
  if (import.meta.env.DEV) {
    console.warn("[i18n] Missing translation", {
      key: path,
      language,
      source: context.source ?? "shared runtime",
      lookup: context.kind ?? "shared",
      dynamic: context.dynamic ?? false,
    });
  }
}

function resolveValue(language: Language, path: string): string | undefined {
  return lookup(dictionaries[language], path);
}

export function translate(
  language: Language,
  path: unknown,
  argument?: TranslationArgument,
): string {
  if (path === null || path === undefined) return "";
  const key = String(path);
  const params = argument && typeof argument === "object" ? argument : undefined;
  const callerFallback = typeof argument === "string" ? argument : undefined;
  const count = typeof params?.count === "number" ? params.count : undefined;
  const pluralPath = count === undefined ? key : `${key}${pluralSuffix(count, language)}`;

  let value = resolveValue(language, pluralPath);
  if (value === undefined && pluralPath !== key) value = resolveValue(language, key);

  if (value === undefined) {
    reportMissingTranslation(key, language, { source: "translate", kind: "shared" });
    if (callerFallback && (language === "en" || /[\u0600-\u06ff]/.test(callerFallback))) {
      value = callerFallback;
    } else {
      value = lookup(dictionaries[language], "common.missingTranslation")
        ?? lookup(dictionaries.ar, "common.missingTranslation")
        ?? "Translation unavailable";
    }
  }

  if (!params) return value;
  return Object.entries(params).reduce((result, [param, replacement]) => {
    if (param.startsWith("_")) return result;
    return result.replace(
      new RegExp(`\\{\\{${param}\\}\\}|\\{${param}\\}`, "g"),
      String(replacement),
    );
  }, value);
}

export function translateCurrent(path: unknown, argument?: TranslationArgument): string {
  return translate(activeLanguage, path, argument);
}

export function translateLegacy(
  namespace: string,
  dictionaries: Record<Language, unknown>,
  language: Language,
  key: string,
): string {
  const value = lookup(dictionaries[language], key);
  if (value !== undefined) return value;
  reportMissingTranslation(`${namespace}.${key}`, language, {
    source: namespace,
    kind: "legacy",
    dynamic: key.includes("${") || key.includes("["),
  });
  // A missing legacy key is a translation defect, not an unknown business value.
  return translate(language, "common.missingTranslation");
}
