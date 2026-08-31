/**
 * PhoneOtpForm component for requesting an SMS verification code.
 * Connected to backend API (POST /api/v1/auth/otp/request).
 *
 * Architectural Glass Skyscraper adaptive Light/Dark mode design.
 */

import React, { useState } from "react";
import { useNavigate, useOutletContext } from "react-router";
import { ArrowLeft, Smartphone, AlertCircle } from "lucide-react";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { COUNTRY_CODES } from "@/features/auth/constants/countryCodes";
import { authApi } from "@/features/auth/api/auth.api";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import { ROUTES } from "@/config/routes";

interface PhoneOtpFormProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function PhoneOtpForm({ lang, onFeedbackMessage }: PhoneOtpFormProps) {
  const navigate = useNavigate();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];

  const [selectedIsoCode, setSelectedIsoCode] = useState("JO");
  const [phoneNumber, setPhoneNumber] = useState("0791234567");
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsLoading(true);

    try {
      const selectedCountry = COUNTRY_CODES.find((c) => c.isoCode === selectedIsoCode) ?? COUNTRY_CODES[0];
      const cleanedLocalPhone = phoneNumber.replace(/\s+/g, "").replace(/^0+/, "");
      const fullPhone = `${selectedCountry.dialCode}${cleanedLocalPhone}`;

      await authApi.requestOtp({ phone: fullPhone, purpose: 0 });

      if (onFeedbackMessage) {
        onFeedbackMessage("SMS verification code sent successfully!");
      }
      navigate(ROUTES.auth.verify, {
        state: { countryCode: selectedCountry.dialCode, phoneNumber },
      });
    } catch (err) {
      setErrorMessage(extractUserFriendlyError(err));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div>
      <div className="mb-6">
        <button
          type="button"
          onClick={() => navigate(ROUTES.auth.login)}
          className={`inline-flex items-center gap-1.5 text-[12.5px] mb-3 transition-colors font-medium cursor-pointer ${
            isDark ? "text-gray-400 hover:text-white" : "text-[#6B7280] hover:text-[#111827]"
          }`}
        >
          <ArrowLeft size={14} />
          <span>{t.backToPasswordLogin}</span>
        </button>
        <h1
          className={`text-2xl font-semibold tracking-tight mb-1 transition-colors duration-350 ${
            isDark ? "text-white" : "text-[#111827]"
          }`}
        >
          {t.phoneOtpTitle}
        </h1>
        <p
          className={`text-[13.5px] transition-colors duration-350 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.phoneOtpSubtitle}
        </p>
      </div>

      {/* Backend API Error Banner */}
      {errorMessage && (
        <div
          className={`mb-4 p-3 border rounded-lg flex items-center gap-2 text-[12.5px] font-medium transition-colors duration-350 ${
            isDark
              ? "bg-red-950/40 border-red-800/50 text-red-300"
              : "bg-red-50 border-red-200 text-red-600"
          }`}
        >
          <AlertCircle size={15} className="shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <FieldLabel isDark={isDark}>{t.phoneLabel}</FieldLabel>
          <div className="flex gap-2">
            <select
              value={selectedIsoCode}
              onChange={(e) => setSelectedIsoCode(e.target.value)}
              className={`w-[100px] h-10 px-2 text-[13px] border rounded-lg outline-none cursor-pointer transition-colors duration-200 ${
                isDark
                  ? "bg-[#161B22]/80 text-gray-100 border-white/10 focus:border-[#656D4A]"
                  : "bg-white text-[#111827] border-[#E5E7EB] focus:border-[#A4AC86]"
              }`}
            >
              {COUNTRY_CODES.map((c) => (
                <option key={c.isoCode} value={c.isoCode}>
                  {c.flag} {c.dialCode}
                </option>
              ))}
            </select>
            <div className="relative flex-1">
              <span
                className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                  isDark ? "text-gray-500" : "text-[#C2C5AA]"
                }`}
              >
                <Smartphone size={15} strokeWidth={1.7} />
              </span>
              <input
                type="tel"
                required
                placeholder="0791234567"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                className={`${inputClass} pl-9`}
              />
            </div>
          </div>
        </div>

        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText={t.sendingCode}
          isDark={isDark}
          className="mt-2"
        >
          {t.sendCodeBtn}
        </ArchitecturalButton>
      </form>
    </div>
  );
}
