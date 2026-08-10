/**
 * LoginForm component for the Login page.
 * Connected to backend API (POST /api/v1/auth/login).
 * Supports returnUrl redirection after successful authentication.
 *
 * Architectural Glass Skyscraper adaptive Light/Dark mode design.
 */

import React, { useState } from "react";
import { useNavigate, useOutletContext, useSearchParams } from "react-router";
import { Mail, Lock, Smartphone, Eye, EyeOff, AlertCircle } from "lucide-react";
import { GoogleLogo } from "@/shared/components/GoogleLogo";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { authApi } from "@/features/auth/api/auth.api";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ROUTES } from "@/config/routes";

interface LoginFormProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function LoginForm({ lang, onFeedbackMessage }: LoginFormProps) {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { login } = useAuth();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];

  // Local Form State
  const [loginTab, setLoginTab] = useState<"password" | "otp">("password");
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsLoading(true);

    try {
      const response = await authApi.login({ 
        emailOrPhone: identifier, 
        password 
      });

      const verifiedUser = await login(response.accessToken, response.user);

      if (onFeedbackMessage) {
        onFeedbackMessage("Authentication successful. Welcome back!");
      }
      
      const returnUrl = searchParams.get("returnUrl");
      let targetPath: string;

      if (returnUrl && returnUrl.startsWith("/")) {
        if (verifiedUser.roleCode === "TENANT" && !returnUrl.startsWith("/tenant")) {
          targetPath = "/tenant/dashboard";
        } else if (verifiedUser.roleCode === "COMPANY_ADMIN" && returnUrl.startsWith("/tenant")) {
          targetPath = ROUTES.dashboard.root;
        } else {
          targetPath = returnUrl;
        }
      } else {
        targetPath = verifiedUser.roleCode === "TENANT" ? "/tenant/dashboard" : ROUTES.dashboard.root;
      }

      navigate(targetPath, { replace: true });
    } catch (err: any) {
      const errorMsg = err?.detail || err?.message || "An unexpected error occurred during login.";
      setErrorMessage(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleSSO = () => {
    setErrorMessage("Google SSO is not connected in this phase.");
  };

  return (
    <div>
      <div className="mb-6">
        <h1
          className={`text-2xl font-semibold tracking-tight mb-1 transition-colors duration-350 ${
            isDark ? "text-white" : "text-[#111827]"
          }`}
        >
          {t.welcomeBack}
        </h1>
        <p
          className={`text-[13.5px] transition-colors duration-350 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.loginSubtitle}
        </p>
      </div>

      {/* Auth Mode Tabs: Password vs. Phone OTP */}
      <div
        className={`flex p-1 rounded-lg border mb-5 transition-colors duration-350 ${
          isDark
            ? "bg-[#0E1116] border-white/10"
            : "bg-[#F3F4F2] border-[#E5E7EB]"
        }`}
      >
        <button
          type="button"
          onClick={() => setLoginTab("password")}
          className={`flex-1 py-1.5 text-[12.5px] font-medium rounded-md flex items-center justify-center gap-2 transition-all cursor-pointer ${
            loginTab === "password"
              ? isDark
                ? "bg-[#161B22] text-white shadow-xs font-semibold border border-white/10"
                : "bg-white text-[#333D29] shadow-xs font-semibold"
              : isDark
              ? "text-gray-400 hover:text-gray-200"
              : "text-[#6B7280] hover:text-[#374151]"
          }`}
        >
          <Lock size={13} strokeWidth={1.8} />
          {t.passwordLoginTab}
        </button>
        <button
          type="button"
          onClick={() => navigate(ROUTES.auth.otp)}
          className={`flex-1 py-1.5 text-[12.5px] font-medium rounded-md flex items-center justify-center gap-2 transition-all cursor-pointer ${
            isDark ? "text-[#6B7280] hover:text-gray-200" : "text-[#6B7280] hover:text-[#374151]"
          }`}
        >
          <Smartphone size={13} strokeWidth={1.8} />
          {t.otpLoginTab}
        </button>
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
        {/* Email or Phone */}
        <div>
          <FieldLabel isDark={isDark}>{t.emailLabel}</FieldLabel>
          <div className="relative">
            <span
              className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                isDark ? "text-gray-500" : "text-[#C2C5AA]"
              }`}
            >
              <Mail size={15} strokeWidth={1.7} />
            </span>
            <input
              type="text"
              required
              placeholder={t.emailPlaceholder}
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
              className={`${inputClass} pl-9`}
            />
          </div>
        </div>

        {/* Password */}
        <div>
          <FieldLabel isDark={isDark}>{t.passwordLabel}</FieldLabel>
          <div className="relative">
            <span
              className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                isDark ? "text-gray-500" : "text-[#C2C5AA]"
              }`}
            >
              <Lock size={15} strokeWidth={1.7} />
            </span>
            <input
              type={showPassword ? "text" : "password"}
              required
              placeholder={t.passwordPlaceholder}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className={`${inputClass} pl-9 pr-10`}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className={`absolute right-3 top-1/2 -translate-y-1/2 transition-colors cursor-pointer ${
                isDark ? "text-gray-500 hover:text-gray-300" : "text-[#9CA3AF] hover:text-[#4B5563]"
              }`}
              aria-label="Toggle password visibility"
            >
              {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
        </div>

        {/* Remember & Forgot Password Links */}
        <div className="flex items-center justify-between text-[13px] pt-0.5">
          <label
            className={`flex items-center gap-2 cursor-pointer transition-colors ${
              isDark ? "text-gray-300" : "text-[#374151]"
            }`}
          >
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              className="w-3.5 h-3.5 rounded accent-[#414833] cursor-pointer"
            />
            <span>{t.rememberMe}</span>
          </label>
          <button
            type="button"
            onClick={() => navigate(ROUTES.auth.forgotPassword)}
            className={`font-medium transition-colors cursor-pointer ${
              isDark ? "text-[#FFD98A] hover:text-amber-200" : "text-[#936639] hover:text-[#7F4F24]"
            }`}
          >
            {t.forgotPassword}
          </button>
        </div>

        {/* Sign In Primary Button */}
        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText={t.signingIn}
          isDark={isDark}
          className="mt-2"
        >
          {t.signInBtn}
        </ArchitecturalButton>

        {/* Divider */}
        <div className="flex items-center gap-3 py-1">
          <div className={`flex-1 h-[1px] ${isDark ? "bg-white/10" : "bg-[#E5E7EB]"}`} />
          <span
            className={`text-[12px] font-medium ${
              isDark ? "text-gray-500" : "text-[#9CA3AF]"
            }`}
          >
            {t.orDivider}
          </span>
          <div className={`flex-1 h-[1px] ${isDark ? "bg-white/10" : "bg-[#E5E7EB]"}`} />
        </div>

        {/* Google SSO Button */}
        <button
          type="button"
          onClick={handleGoogleSSO}
          className={`w-full h-10 font-medium text-[13.5px] rounded-lg transition-colors flex items-center justify-center gap-2.5 cursor-pointer border ${
            isDark
              ? "bg-[#161B22]/60 hover:bg-[#161B22] text-gray-200 border-white/10 hover:border-white/20"
              : "bg-white hover:bg-[#F9FAFB] text-[#374151] border-[#E5E7EB] hover:border-[#D1D5DB]"
          }`}
        >
          <GoogleLogo />
          <span>{t.googleSSO}</span>
        </button>

        {/* Switch to Register */}
        <p
          className={`text-[13px] text-center pt-2 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.noAccount}{" "}
          <button
            type="button"
            onClick={() => navigate(ROUTES.auth.register)}
            className={`font-semibold hover:underline cursor-pointer ${
              isDark ? "text-[#FFD98A]" : "text-[#414833]"
            }`}
          >
            {t.signUpLink}
          </button>
        </p>
      </form>
    </div>
  );
}
