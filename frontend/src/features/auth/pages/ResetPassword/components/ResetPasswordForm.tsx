/**
 * ResetPasswordForm component.
 * Connected to Architectural Glass Skyscraper design system.
 */

import React, { useState } from "react";
import { useNavigate, useOutletContext } from "react-router";
import { KeyRound, Lock, Eye, EyeOff } from "lucide-react";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { isPasswordMismatch } from "@/features/auth/validation/resetPassword.schema";
import { ROUTES } from "@/config/routes";

interface ResetPasswordFormProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function ResetPasswordForm({ lang, onFeedbackMessage }: ResetPasswordFormProps) {
  const navigate = useNavigate();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const isMismatch = isPasswordMismatch(newPassword, confirmPassword);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isMismatch || !newPassword) return;
    setIsLoading(true);
    setTimeout(() => {
      setIsLoading(false);
      if (onFeedbackMessage) {
        onFeedbackMessage(t.passwordResetSuccess);
      }
      navigate(ROUTES.auth.login);
    }, 1200);
  };

  return (
    <div>
      <div className="mb-6">
        <h1
          className={`text-2xl font-semibold tracking-tight mb-1 transition-colors duration-350 ${
            isDark ? "text-white" : "text-[#111827]"
          }`}
        >
          {t.resetTitle}
        </h1>
        <p
          className={`text-[13.5px] transition-colors duration-350 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.resetSubtitle}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <FieldLabel isDark={isDark}>{t.newPasswordLabel}</FieldLabel>
          <div className="relative">
            <span
              className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                isDark ? "text-gray-500" : "text-[#C2C5AA]"
              }`}
            >
              <KeyRound size={15} />
            </span>
            <input
              type={showPassword ? "text" : "password"}
              required
              placeholder={t.newPasswordPlaceholder}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className={`${inputClass} pl-9 pr-10`}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className={`absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer ${
                isDark ? "text-gray-500 hover:text-gray-300" : "text-[#9CA3AF] hover:text-[#4B5563]"
              }`}
            >
              {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
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
              type="password"
              required
              placeholder={t.confirmPasswordPlaceholder}
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className={`${inputClass} pl-9 ${
                isMismatch ? "border-red-400 focus:border-red-500" : ""
              }`}
            />
          </div>
          {isMismatch && (
            <span className="text-[11.5px] text-red-500 mt-1 block">
              {t.passwordMismatch}
            </span>
          )}
        </div>

        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText={t.updating}
          disabled={isMismatch || !newPassword}
          isDark={isDark}
        >
          {t.saveNewPasswordBtn}
        </ArchitecturalButton>
      </form>
    </div>
  );
}
