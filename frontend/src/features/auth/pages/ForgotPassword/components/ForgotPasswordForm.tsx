import React, { useState } from "react";
import { useNavigate, useOutletContext } from "react-router";
import { ArrowLeft, Mail, Smartphone, AlertCircle, CheckCircle2 } from "lucide-react";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { COUNTRY_CODES } from "@/features/auth/constants/countryCodes";
import { authApi } from "@/features/auth/api/auth.api";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import { ROUTES } from "@/config/routes";

interface ForgotPasswordFormProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function ForgotPasswordForm({ lang, onFeedbackMessage }: ForgotPasswordFormProps) {
  const navigate = useNavigate();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];
  const [recoveryMethod, setRecoveryMethod] = useState<"email" | "phone">("email");
  const [contact, setContact] = useState("");
  const [countryIso, setCountryIso] = useState("JO");
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [emailSubmitted, setEmailSubmitted] = useState(false);

  const selectMethod = (method: "email" | "phone") => {
    setRecoveryMethod(method);
    setContact("");
    setErrorMessage(null);
    setEmailSubmitted(false);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    setIsLoading(true);
    try {
      let identifier = contact.trim();
      if (recoveryMethod === "phone") {
        const country = COUNTRY_CODES.find((item) => item.isoCode === countryIso) ?? COUNTRY_CODES[0];
        const compact = identifier.replace(/[\s().-]/g, "");
        identifier = compact.startsWith("+") || compact.startsWith("00")
          ? compact
          : `${country.dialCode}${compact.replace(/^0+/, "")}`;
      }

      await authApi.requestPasswordReset({
        deliveryMethod: recoveryMethod === "email" ? "Email" : "Phone",
        identifier,
      });

      if (onFeedbackMessage) onFeedbackMessage(t.resetRequestGeneric);
      if (recoveryMethod === "email") {
        setEmailSubmitted(true);
      } else {
        navigate(ROUTES.auth.passwordResetVerify, { state: { phone: identifier } });
      }
    } catch (error) {
      setErrorMessage(extractUserFriendlyError(error));
    } finally {
      setIsLoading(false);
    }
  };

  if (emailSubmitted) {
    return (
      <div className="space-y-5 text-center">
        <CheckCircle2 className="mx-auto h-12 w-12 text-emerald-500" />
        <h1 className="text-xl font-semibold">{t.checkResetEmail}</h1>
        <p className="text-sm text-muted-foreground">{t.resetRequestGeneric}</p>
        <ArchitecturalButton type="button" onClick={() => navigate(ROUTES.auth.login)} isDark={isDark}>
          {t.signInLink}
        </ArchitecturalButton>
      </div>
    );
  }

  return (
    <div>
      <button type="button" onClick={() => navigate(ROUTES.auth.login)} className="mb-3 inline-flex items-center gap-1.5 text-xs font-medium">
        <ArrowLeft size={14} /> {t.signInLink}
      </button>
      <h1 className="mb-1 text-2xl font-semibold">{t.forgotTitle}</h1>
      <p className="mb-5 text-sm text-muted-foreground">{t.forgotSubtitle}</p>

      <div className="mb-5 flex rounded-lg border p-1">
        {(["email", "phone"] as const).map((method) => (
          <button key={method} type="button" onClick={() => selectMethod(method)}
            className={`flex-1 rounded-md py-1.5 text-xs font-medium ${recoveryMethod === method ? "bg-background shadow" : "text-muted-foreground"}`}>
            {method === "email" ? t.methodEmail : t.methodPhone}
          </button>
        ))}
      </div>

      {errorMessage && <div role="alert" className="mb-4 flex gap-2 rounded-lg border border-red-300 bg-red-50 p-3 text-xs text-red-700"><AlertCircle size={15} />{errorMessage}</div>}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <FieldLabel isDark={isDark}>{recoveryMethod === "email" ? t.emailLabel : t.phoneLabel}</FieldLabel>
          <div className="flex gap-2">
            {recoveryMethod === "phone" && (
              <select value={countryIso} onChange={(event) => setCountryIso(event.target.value)} className={`${inputClass} w-28`}>
                {COUNTRY_CODES.map((country) => <option key={country.isoCode} value={country.isoCode}>{country.flag} {country.dialCode}</option>)}
              </select>
            )}
            <div className="relative flex-1">
              <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">
                {recoveryMethod === "email" ? <Mail size={15} /> : <Smartphone size={15} />}
              </span>
              <input type={recoveryMethod === "email" ? "email" : "tel"} required
                value={contact} onChange={(event) => setContact(event.target.value)}
                placeholder={recoveryMethod === "email" ? t.emailPlaceholder : t.phonePlaceholder}
                className={`${inputClass} pl-9`} />
            </div>
          </div>
        </div>
        <ArchitecturalButton type="submit" isLoading={isLoading} loadingText={t.sendingCode} isDark={isDark}>
          {t.sendResetBtn}
        </ArchitecturalButton>
      </form>
    </div>
  );
}
