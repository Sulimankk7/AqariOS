import { useTranslation } from "./I18nProvider";
import { reportMissingTranslation, TranslationArgument } from "./runtime";

type Translator = (key: string, argument?: TranslationArgument) => string;

export type EntityLabelDomain =
  | "status"
  | "role"
  | "paymentMethod"
  | "utilitySyncStatus"
  | "utilityProviderAvailability"
  | "utilityPaymentStatus"
  | "utilityType"
  | "companyType"
  | "documentStatus"
  | "notificationType";

const aliases: Partial<Record<EntityLabelDomain, Record<string, string>>> = {
  status: {
    canceled: "cancelled",
    overdue: "overdueUnpaid",
    overdueunpaid: "overdueUnpaid",
    partiallypaid: "partiallyPaid",
    pendingverification: "pendingVerification",
    inprogress: "inProgress",
    pastdue: "pastDue",
  },
  role: {
    systemadmin: "systemAdmin",
    companyadmin: "companyAdmin",
    propertymanager: "propertyManager",
  },
  paymentMethod: {
    banktransfer: "bankTransfer",
    efawateercom: "efawateercom",
    cliq: "cliq",
  },
  utilitySyncStatus: {
    neversynced: "neverSynced",
    providererror: "providerError",
    ratelimited: "rateLimited",
    invalidaccount: "invalidAccount",
  },
  utilityProviderAvailability: {
    available: "available",
    unavailable: "unavailable",
    unknown: "unknown",
  },
};

function normalized(value: unknown, domain: EntityLabelDomain): string {
  if (domain === "paymentMethod" && Number.isInteger(Number(value))) {
    return ["cash", "banktransfer", "cheque", "efawateercom", "cliq"][Number(value)] ?? "";
  }
  return String(value ?? "").trim().replace(/[^a-zA-Z0-9]/g, "").toLowerCase();
}

export function entityLabel(
  domain: EntityLabelDomain,
  value: unknown,
  t: Translator,
  language: "ar" | "en",
): string {
  const raw = normalized(value, domain);
  // An absent value is not an unsupported enum; preserve the product's "not specified" meaning.
  if (!raw) return t("common.notSpecified");
  const key = aliases[domain]?.[raw] ?? raw;
  const translationKey = `enums.${domain}.${key}`;
  const translated = t(translationKey);
  if (translated === t("common.missingTranslation")) {
    reportMissingTranslation(translationKey, language);
    // The value is known, but its dictionary entry is missing. Do not mislabel it as unknown.
    return translated;
  }
  return translated;
}

export function useEntityLabel() {
  const { t, language } = useTranslation();
  return (domain: EntityLabelDomain, value: unknown) => entityLabel(domain, value, t, language);
}
