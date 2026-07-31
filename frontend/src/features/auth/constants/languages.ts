/**
 * Auth feature — preferred system language options.
 * Exact list from the Figma-generated design — do not modify.
 */

export interface LanguageOption {
  value: string;
  label: string;
}

export const LANGUAGES: LanguageOption[] = [
  { value: "en", label: "English (US)" },
  { value: "ar", label: "العربية (Arabic)" },
  { value: "fr", label: "Français (French)" },
];
