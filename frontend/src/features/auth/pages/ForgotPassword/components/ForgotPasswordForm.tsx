/**
 * ForgotPasswordForm component.
 * Connected to Architectural Glass Skyscraper design system.
 */

import React, { useState } from "react";
import { useNavigate, useOutletContext } from "react-router";
import { ArrowLeft, Mail, Smartphone } from "lucide-react";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
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
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setTimeout(() => {
      setIsLoading(false);
      if (onFeedbackMessage) {
        onFeedbackMessage(t.resetInstructionsSent);
      }
      navigate(ROUTES.auth.resetPassword);
    }, 1200);
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
          <span>{t.signInLink}</span>
        </button>
        <h1
          className={`text-2xl font-semibold tracking-tight mb-1 transition-colors duration-350 ${
            isDark ? "text-white" : "text-[#111827]"
          }`}
        >
          {t.forgotTitle}
        </h1>
        <p
          className={`text-[13.5px] transition-colors duration-350 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.forgotSubtitle}
        </p>
      </div>

      {/* Method Selector: Email vs. Phone */}
      <div
        className={`flex p-1 rounded-lg border mb-5 transition-colors duration-350 ${
          isDark ? "bg-[#0E1116] border-white/10" : "bg-[#F3F4F2] border-[#E5E7EB]"
        }`}
      >
        <button
          type="button"
          onClick={() => setRecoveryMethod("email")}
          className={`flex-1 py-1.5 text-[12.5px] font-medium rounded-md transition-all cursor-pointer ${
            recoveryMethod === "email"
              ? isDark
                ? "bg-[#161B22] text-white shadow-xs font-semibold border border-white/10"
                : "bg-white text-[#333D29] shadow-xs font-semibold"
              : isDark
              ? "text-gray-400"
              : "text-[#6B7280]"
          }`}
        >
          {t.methodEmail}
        </button>
        <button
          type="button"
          onClick={() => setRecoveryMethod("phone")}
          className={`flex-1 py-1.5 text-[12.5px] font-medium rounded-md transition-all cursor-pointer ${
            recoveryMethod === "phone"
              ? isDark
                ? "bg-[#161B22] text-white shadow-xs font-semibold border border-white/10"
                : "bg-white text-[#333D29] shadow-xs font-semibold"
              : isDark
              ? "text-gray-400"
              : "text-[#6B7280]"
          }`}
        >
          {t.methodPhone}
        </button>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <FieldLabel isDark={isDark}>
            {recoveryMethod === "email" ? t.emailLabel : t.phoneLabel}
          </FieldLabel>
          <div className="relative">
            <span
              className={`absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none ${
                isDark ? "text-gray-500" : "text-[#C2C5AA]"
              }`}
            >
              {recoveryMethod === "email" ? <Mail size={15} /> : <Smartphone size={15} />}
            </span>
            <input
              type={recoveryMethod === "email" ? "email" : "tel"}
              required
              placeholder={
                recoveryMethod === "email" ? t.emailPlaceholder : t.phonePlaceholder
              }
              value={contact}
              onChange={(e) => setContact(e.target.value)}
              className={`${inputClass} pl-9`}
            />
          </div>
        </div>

        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText={t.sendingCode}
          isDark={isDark}
          className="mt-2"
        >
          {t.sendResetBtn}
        </ArchitecturalButton>
      </form>
    </div>
  );
}
