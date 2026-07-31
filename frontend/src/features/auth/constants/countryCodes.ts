/**
 * Auth feature — country options catalog.
 * Primary default: Jordan (JO / +962).
 */

export interface CountryOption {
  name: string;
  isoCode: string;
  dialCode: string;
  flag: string;
  label: string;
  /** Alias for backward compatibility with dialCode */
  code: string;
}

export const COUNTRY_CODES: CountryOption[] = [
  { name: "Jordan",               isoCode: "JO", dialCode: "+962", code: "+962", flag: "🇯🇴", label: "JOR (+962)" },
  { name: "United Arab Emirates", isoCode: "AE", dialCode: "+971", code: "+971", flag: "🇦🇪", label: "UAE (+971)" },
  { name: "Saudi Arabia",        isoCode: "SA", dialCode: "+966", code: "+966", flag: "🇸🇦", label: "KSA (+966)" },
  { name: "United States",        isoCode: "US", dialCode: "+1",   code: "+1",   flag: "🇺🇸", label: "USA (+1)" },
  { name: "United Kingdom",       isoCode: "GB", dialCode: "+44",  code: "+44",  flag: "🇬🇧", label: "UK (+44)" },
  { name: "Kuwait",               isoCode: "KW", dialCode: "+965", code: "+965", flag: "🇰🇼", label: "KWT (+965)" },
  { name: "Qatar",                isoCode: "QA", dialCode: "+974", code: "+974", flag: "🇶🇦", label: "QAT (+974)" },
  { name: "Bahrain",              isoCode: "BH", dialCode: "+973", code: "+973", flag: "🇧🇭", label: "BHR (+973)" },
  { name: "Oman",                 isoCode: "OM", dialCode: "+968", code: "+968", flag: "🇴🇲", label: "OMN (+968)" },
  { name: "Egypt",                isoCode: "EG", dialCode: "+20",  code: "+20",  flag: "🇪🇬", label: "EGY (+20)" },
];
