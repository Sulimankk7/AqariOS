/**
 * RegisterForm component for the Register page.
 * Connected to backend API (POST /api/v1/auth/register).
 *
 * Architectural Glass Skyscraper adaptive Light/Dark mode design.
 */

import React, { useState } from "react";
import { useNavigate, useOutletContext } from "react-router";
import { User, Building2, ChevronDown, Mail, Smartphone, Lock, Eye, EyeOff, AlertCircle } from "lucide-react";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { COUNTRY_CODES } from "@/features/auth/constants/countryCodes";
import { COMPANY_TYPES } from "@/features/auth/constants/companyTypes";
import { LANGUAGES } from "@/features/auth/constants/languages";
import { CompanyType } from "@/features/auth/types/auth.types";
import { isPasswordMismatch } from "@/features/auth/validation/resetPassword.schema";
import { authApi } from "@/features/auth/api/auth.api";
import { ROUTES } from "@/config/routes";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";

interface RegisterFormProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function RegisterForm({ lang, onFeedbackMessage }: RegisterFormProps) {
  const navigate = useNavigate();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];

  // Local Form State — Default: Jordan (JO / +962)
  const [fullName, setFullName] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [companyType, setCompanyType] = useState<CompanyType | "">("");
  const [email, setEmail] = useState("");
  const [selectedIsoCode, setSelectedIsoCode] = useState("JO");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [prefLanguage, setPrefLanguage] = useState("en");
  const [agreedTerms, setAgreedTerms] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const isMismatch = isPasswordMismatch(password, confirmPassword);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isLoading || isMismatch || !agreedTerms) return;

    setErrorMessage(null);
    setIsLoading(true);

    try {
      const selectedCountry = COUNTRY_CODES.find((c) => c.isoCode === selectedIsoCode) ?? COUNTRY_CODES[0];
      const cleanedLocalPhone = phone.replace(/\s+/g, "").replace(/^0+/, "");
      const fullPhone = phone.trim() ? `${selectedCountry.dialCode}${cleanedLocalPhone}` : undefined;

      await authApi.register({
        fullName: fullName.trim(),
        companyName: companyName.trim(),
        displayName: displayName.trim() || undefined,
        companyType: companyType !== "" ? companyType : CompanyType.IndividualOwner,
        email: email.trim() || undefined,
        phone: fullPhone,
        password,
        countryCode: selectedIsoCode,
        preferredLanguage: prefLanguage || "ar",
      });

      if (onFeedbackMessage) {
        onFeedbackMessage(t.registrationSubmitted);
      }
      navigate(ROUTES.auth.login, { replace: true });
    } catch (err: any) {
      setErrorMessage(extractUserFriendlyError(err, t.registrationError));
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleSSO = () => {
    setErrorMessage(t.googleUnavailable);
  };

  return (
    <div>
      <div className="mb-5">
        <h1
          className={`text-2xl font-semibold tracking-tight mb-1 transition-colors duration-350 ${
            isDark ? "text-white" : "text-[#111827]"
          }`}
        >
          {t.createAccountTitle}
        </h1>
        <p
          className={`text-[13.5px] transition-colors duration-350 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.createAccountSubtitle}
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

      <form onSubmit={handleSubmit} className="space-y-3.5">
        {/* Grid Row 1: Full Name + Company Name */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <FieldLabel isDark={isDark}>{t.fullNameLabel}</FieldLabel>
            <div className="relative">
              <span
                className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                  isDark ? "text-gray-500" : "text-[#C2C5AA]"
                }`}
              >
                <User size={15} />
              </span>
              <input
                type="text"
                required
                placeholder={t.fullNamePlaceholder}
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                className={`${inputClass} pl-9`}
              />
            </div>
          </div>

          <div>
            <FieldLabel isDark={isDark}>{t.companyNameLabel}</FieldLabel>
            <div className="relative">
              <span
                className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                  isDark ? "text-gray-500" : "text-[#C2C5AA]"
                }`}
              >
                <Building2 size={15} />
              </span>
              <input
                type="text"
                required
                placeholder={t.companyNamePlaceholder}
                value={companyName}
                onChange={(e) => setCompanyName(e.target.value)}
                className={`${inputClass} pl-9`}
              />
            </div>
          </div>
        </div>

        {/* Grid Row 2: Display Name (Optional) + Company Type */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <FieldLabel isDark={isDark} optional={t.displayNameOptional}>
              {t.displayNameLabel}
            </FieldLabel>
            <input
              type="text"
              placeholder={t.displayNamePlaceholder}
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              className={inputClass}
            />
          </div>

          <div>
            <FieldLabel isDark={isDark}>{t.companyTypeLabel}</FieldLabel>
            <div className="relative">
              <select
                required
                value={companyType}
                onChange={(e) => setCompanyType(Number(e.target.value) as CompanyType)}
                className={`${inputClass} appearance-none cursor-pointer pr-8`}
              >
                <option value="" disabled>
                  {t.companyTypeSelect}
                </option>
                {COMPANY_TYPES.map((ct) => (
                  <option key={ct.value} value={ct.value}>
                    {ct.label}
                  </option>
                ))}
              </select>
              <ChevronDown
                size={14}
                className={`absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                  isDark ? "text-gray-500" : "text-[#9CA3AF]"
                }`}
              />
            </div>
          </div>
        </div>

        {/* Email */}
        <div>
          <FieldLabel isDark={isDark}>{t.emailLabel}</FieldLabel>
          <div className="relative">
            <span
              className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                isDark ? "text-gray-500" : "text-[#C2C5AA]"
              }`}
            >
              <Mail size={15} />
            </span>
            <input
              type="email"
              required
              placeholder={t.emailPlaceholder}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className={`${inputClass} pl-9`}
            />
          </div>
        </div>

        {/* Phone Number with Country Code Selector */}
        <div>
          <FieldLabel isDark={isDark}>{t.phoneLabel}</FieldLabel>
          <div className="flex gap-2">
            <select
              value={selectedIsoCode}
              onChange={(e) => setSelectedIsoCode(e.target.value)}
              className={`w-[105px] h-10 px-2 text-[13px] border rounded-lg outline-none cursor-pointer transition-colors duration-200 ${
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
                <Smartphone size={15} />
              </span>
              <input
                type="tel"
                required
                placeholder="79 123 4567"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className={`${inputClass} pl-9`}
              />
            </div>
          </div>
        </div>

        {/* Grid Row 3: Password + Confirm Password */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <FieldLabel isDark={isDark}>{t.passwordLabel}</FieldLabel>
            <div className="relative">
              <span
                className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                  isDark ? "text-gray-500" : "text-[#C2C5AA]"
                }`}
              >
                <Lock size={15} />
              </span>
              <input
                type={showPassword ? "text" : "password"}
                required
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={`${inputClass} pl-9 pr-9`}
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className={`absolute right-2.5 top-1/2 -translate-y-1/2 cursor-pointer ${
                  isDark ? "text-gray-500 hover:text-gray-300" : "text-[#9CA3AF]"
                }`}
              >
                {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
          </div>

          <div>
            <FieldLabel isDark={isDark}>{t.confirmPasswordLabel}</FieldLabel>
            <div className="relative">
              <span
                className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                  isDark ? "text-gray-500" : "text-[#C2C5AA]"
                }`}
              >
                <Lock size={15} />
              </span>
              <input
                type={showConfirmPassword ? "text" : "password"}
                required
                placeholder="••••••••"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className={`${inputClass} pl-9 pr-9 ${
                  isMismatch ? "border-red-400 focus:border-red-500" : ""
                }`}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                className={`absolute right-2.5 top-1/2 -translate-y-1/2 cursor-pointer ${
                  isDark ? "text-gray-500 hover:text-gray-300" : "text-[#9CA3AF]"
                }`}
              >
                {showConfirmPassword ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
            {isMismatch && (
              <span className="text-[11px] text-red-500 mt-1 block">
                {t.passwordMismatch}
              </span>
            )}
          </div>
        </div>

        {/* Preferred System Language */}
        <div>
          <FieldLabel isDark={isDark}>{t.prefLangLabel}</FieldLabel>
          <div className="relative">
            <select
              value={prefLanguage}
              onChange={(e) => setPrefLanguage(e.target.value)}
              className={`${inputClass} appearance-none cursor-pointer pr-8`}
            >
              {LANGUAGES.map((l) => (
                <option key={l.value} value={l.value}>
                  {l.label}
                </option>
              ))}
            </select>
            <ChevronDown
              size={14}
              className={`absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                isDark ? "text-gray-500" : "text-[#9CA3AF]"
              }`}
            />
          </div>
        </div>

        {/* Terms Agreement Checkbox */}
        <label className="flex items-start gap-2.5 pt-1 cursor-pointer">
          <input
            type="checkbox"
            checked={agreedTerms}
            onChange={(e) => setAgreedTerms(e.target.checked)}
            className="w-4 h-4 mt-0.5 rounded accent-[#414833] cursor-pointer shrink-0"
          />
          <span
            className={`text-[12.5px] leading-snug ${
              isDark ? "text-gray-300" : "text-[#374151]"
            }`}
          >
            {t.agreeTermsPrefix}
            <a
              href="#"
              className={`font-medium hover:underline ${
                isDark ? "text-[#FFD98A]" : "text-[#414833]"
              }`}
            >
              {t.terms}
            </a>
            {t.agreeTermsAnd}
            <a
              href="#"
              className={`font-medium hover:underline ${
                isDark ? "text-[#FFD98A]" : "text-[#414833]"
              }`}
            >
              {t.privacy}
            </a>
          </span>
        </label>

        {/* Submit Primary Button (Architectural Glass Panel) */}
        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText={t.creatingAccount}
          disabled={!agreedTerms || isMismatch}
          isDark={isDark}
          className="mt-2"
        >
          {t.createAccountBtn}
        </ArchitecturalButton>

        

        <p
          className={`text-[13px] text-center pt-1 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.alreadyHaveAccount}{" "}
          <button
            type="button"
            onClick={() => navigate(ROUTES.auth.login)}
            className={`font-semibold hover:underline cursor-pointer ${
              isDark ? "text-[#FFD98A]" : "text-[#414833]"
            }`}
          >
            {t.signInLink}
          </button>
        </p>
      </form>
    </div>
  );
}
