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
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setFieldErrors({});
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
      if (err?.code === "ACCOUNT_NOT_ACTIVE") {
        setErrorMessage(t.pendingApproval);
      } else if (err?.validationErrors && Object.keys(err.validationErrors).length > 0) {
        setFieldErrors(err.validationErrors);
      } else {
        const errorMsg = err?.detail || err?.message || "An unexpected error occurred during login.";
        setErrorMessage(errorMsg);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleSSO = () => {
    setErrorMessage("Google SSO is not connected in this phase.");
  };

  const unmappedErrors = Object.entries(fieldErrors)
    .filter(([key]) => key !== "EmailOrPhone" && key !== "Password")
    .flatMap(([_, msgs]) => msgs);

  return (
    <div>
      <div className="mb-6">
        <h1
          className="text-2xl font-semibold tracking-tight mb-1 text-foreground transition-colors duration-350"
        >
          {t.welcomeBack}
        </h1>
        <p
          className="text-[13.5px] font-medium text-muted-foreground transition-colors duration-350"
        >
          {t.loginSubtitle}
        </p>
      </div>

      {/* Auth Mode Tabs: Password vs. Phone OTP */}
      <div
        className="flex p-1 rounded-lg border border-outline-variant bg-surface-container mb-5 transition-colors duration-350"
      >
        <button
          type="button"
          onClick={() => setLoginTab("password")}
          className={`flex-1 py-1.5 text-[12.5px] font-medium rounded-md flex items-center justify-center gap-2 transition-all cursor-pointer ${
            loginTab === "password"
              ? isDark
                ? "bg-card text-foreground shadow-e1 font-semibold border border-outline-variant"
                : "bg-card text-foreground shadow-e1 font-semibold border border-outline-variant"
              : isDark
              ? "text-muted-foreground hover:text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          <Lock size={13} strokeWidth={1.8} />
          {t.passwordLoginTab}
        </button>
        <button
          type="button"
          onClick={() => navigate(ROUTES.auth.otp)}
          className={`flex-1 py-1.5 text-[12.5px] font-medium rounded-md flex items-center justify-center gap-2 transition-all cursor-pointer ${
            "text-muted-foreground hover:text-foreground"
          }`}
        >
          <Smartphone size={13} strokeWidth={1.8} />
          {t.otpLoginTab}
        </button>
      </div>

      {/* Backend API Error Banner */}
      {(errorMessage || unmappedErrors.length > 0) && (
        <div
          className={`mb-4 p-3 border rounded-lg flex items-start gap-2 text-[12.5px] font-medium transition-colors duration-350 ${
            isDark
              ? "bg-red-950/40 border-red-800/50 text-red-300"
              : "bg-red-50 border-red-200 text-red-600"
          }`}
        >
          <AlertCircle size={15} className="shrink-0 mt-0.5" />
          <div className="flex flex-col">
            {errorMessage && <span>{errorMessage}</span>}
            {unmappedErrors.length > 0 && (
              <>
                {!errorMessage && <span>{t.fixErrors}</span>}
                <ul className="list-disc list-inside mt-1">
                  {unmappedErrors.map((err, i) => (
                    <li key={i}>{err}</li>
                  ))}
                </ul>
              </>
            )}
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Email or Phone */}
        <div>
          <FieldLabel isDark={isDark}>{t.emailLabel}</FieldLabel>
          <div className="relative">
            <span
              className="absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none text-muted-foreground"
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
          {fieldErrors.EmailOrPhone && (
            <span className="text-[11px] text-red-500 mt-1 block">
              {fieldErrors.EmailOrPhone.join(", ")}
            </span>
          )}
        </div>

        {/* Password */}
        <div>
          <FieldLabel isDark={isDark}>{t.passwordLabel}</FieldLabel>
          <div className="relative">
            <span
              className="absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none text-muted-foreground"
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
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
              aria-label="Toggle password visibility"
            >
              {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          {fieldErrors.Password && (
            <span className="text-[11px] text-red-500 mt-1 block">
              {fieldErrors.Password.join(", ")}
            </span>
          )}
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
          <div className="flex-1 h-px bg-outline-variant" />
          <span
            className="text-[12px] font-medium text-muted-foreground"
          >
            {t.orDivider}
          </span>
          <div className="flex-1 h-px bg-outline-variant" />
        </div>

        {/* Google SSO Button */}
        <button
          type="button"
          onClick={handleGoogleSSO}
          className={`w-full h-10 font-medium text-[13.5px] rounded-lg transition-colors flex items-center justify-center gap-2.5 cursor-pointer border ${
            isDark
              ? "bg-card hover:bg-surface-container-high text-foreground border-outline-variant hover:border-outline"
              : "bg-card hover:bg-surface-container-low text-foreground border-outline-variant hover:border-outline"
          }`}
        >
          <GoogleLogo />
          <span>{t.googleSSO}</span>
        </button>

        {/* Switch to Register */}
        <p
          className="text-[13px] font-medium text-center pt-2 text-muted-foreground"
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
