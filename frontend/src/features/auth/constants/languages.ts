/**
 * Auth feature — preferred system language options.
 * Exact list from the Figma-generated design — do not modify.
 */

export interface LanguageOption {
  value: string;
  label: string;
}

export const LANGUAGES: LanguageOption[] = [
  { value: "ar", label: "العربية" },
  { value: "en", label: "English" },
];
