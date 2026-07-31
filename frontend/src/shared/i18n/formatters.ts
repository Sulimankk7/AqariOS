/**
 * Internationalization Formatters for AqariOS.
 * Provides locale-aware Currency, Date, Time, and Number formatting according to Jordan real estate standards.
 */

export interface FormatCurrencyOptions {
  currency?: string;
  locale?: string;
  minimumFractionDigits?: number;
  maximumFractionDigits?: number;
}

/**
 * Format monetary amount with locale currency rules.
 * Default currency is JOD (Jordanian Dinar).
 */
export function formatCurrency(
  amount: number,
  options: FormatCurrencyOptions = {}
): string {
  const {
    currency = "JOD",
    locale = "en-US",
    minimumFractionDigits = 2,
    maximumFractionDigits = 2,
  } = options;

  try {
    return new Intl.NumberFormat(locale, {
      style: "currency",
      currency,
      minimumFractionDigits,
      maximumFractionDigits,
    }).format(amount);
  } catch {
    return `${currency} ${amount.toFixed(2)}`;
  }
}

/**
 * Format Date into localized long, medium, or short date string.
 */
export function formatDate(
  date: Date | string | number,
  locale: string = "en-US",
  options?: Intl.DateTimeFormatOptions
): string {
  const d = new Date(date);
  const defaultOptions: Intl.DateTimeFormatOptions = options || {
    year: "numeric",
    month: "long",
    day: "numeric",
  };

  try {
    return new Intl.DateTimeFormat(locale, defaultOptions).format(d);
  } catch {
    return d.toLocaleDateString();
  }
}

/**
 * Format Time into localized 12-hour or 24-hour time string.
 */
export function formatTime(
  date: Date | string | number,
  locale: string = "en-US",
  options?: Intl.DateTimeFormatOptions
): string {
  const d = new Date(date);
  const defaultOptions: Intl.DateTimeFormatOptions = options || {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  };

  try {
    return new Intl.DateTimeFormat(locale, defaultOptions).format(d);
  } catch {
    return d.toLocaleTimeString();
  }
}

/**
 * Format raw numbers with localized thousands separators and Arabic digits when applicable.
 */
export function formatNumber(
  num: number,
  locale: string = "en-US",
  options?: Intl.NumberFormatOptions
): string {
  try {
    return new Intl.NumberFormat(locale, options).format(num);
  } catch {
    return String(num);
  }
}
