/**
 * OtpGrid component.
 * Connected to backend API (POST /api/v1/auth/otp/verify).
 *
 * Architectural Glass Skyscraper adaptive Light/Dark mode design.
 */

import React, { useState, useRef, useEffect } from "react";
import { useNavigate, useLocation, useOutletContext } from "react-router";
import { AlertCircle } from "lucide-react";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { useOtpTimer } from "@/features/auth/hooks/useOtpTimer";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { authApi } from "@/features/auth/api/auth.api";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { ROUTES } from "@/config/routes";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import { ApiError } from "@/shared/lib/http";

interface OtpGridProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function OtpGrid({ lang, onFeedbackMessage }: OtpGridProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];

  // Retrieve state passed from PhoneOtp page or fallback
  const state = location.state as { countryCode?: string; phoneNumber?: string } | null;
  const countryCode = state?.countryCode ?? "+962";
  const phoneNumber = state?.phoneNumber ?? "0791234567";

  const [digits, setDigits] = useState(["", "", "", "", "", ""]);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  const timer = useOtpTimer(60);

  useEffect(() => {
    timer.start();
  }, []);

  const handleDigitChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return;
    const newDigits = [...digits];
    newDigits[index] = value.slice(-1);
    setDigits(newDigits);

    // Auto-focus next input
    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Backspace" && !digits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    e.preventDefault();
    const pastedData = e.clipboardData.getData("text").trim().slice(0, 6);
    if (/^\d+$/.test(pastedData)) {
      const charArray = pastedData.split("");
      const newDigits = [...digits];
      charArray.forEach((d, i) => {
        if (i < 6) newDigits[i] = d;
      });
      setDigits(newDigits);
      inputRefs.current[Math.min(charArray.length, 5)]?.focus();
    }
  };

  const handleResend = async () => {
    setErrorMessage(null);
    try {
      const fullPhone = `${countryCode}${phoneNumber.replace(/\s+/g, "").replace(/^0+/, "")}`;
      await authApi.requestOtp({ phone: fullPhone, purpose: 0 });
      timer.start(60);
      if (onFeedbackMessage) {
        onFeedbackMessage(t.otpResent);
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 429) {
        const retryAfter = err.retryAfterSeconds ?? 60;
        timer.start(retryAfter);
        setErrorMessage(extractUserFriendlyError(err, t.otpError));
      } else {
        setErrorMessage(extractUserFriendlyError(err, t.otpError));
      }
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsLoading(true);

    try {
      const fullPhone = `${countryCode}${phoneNumber.replace(/\s+/g, "").replace(/^0+/, "")}`;
      const code = digits.join("");

      const response = await authApi.verifyOtp({
        phone: fullPhone,
        code,
        purpose: 0,
      });

      const verifiedUser = await login(response.accessToken, response.user);

      if (onFeedbackMessage) {
        onFeedbackMessage(t.verificationSuccess);
      }

      const targetPath = verifiedUser.roleCode === "TENANT" ? "/tenant/dashboard" : ROUTES.dashboard.root;
      navigate(targetPath, { replace: true });
    } catch (err: any) {
      setErrorMessage(extractUserFriendlyError(err, t.otpError));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div>
      <div className="mb-6">
        <h1
          className={`text-2xl font-semibold tracking-tight mb-1 transition-colors duration-350 ${
            isDark ? "text-white" : "text-[#111827]"
          }`}
        >
          {t.otpVerifyTitle}
        </h1>
        <p
          className={`text-[13.5px] transition-colors duration-350 ${
            isDark ? "text-gray-400" : "text-[#6B7280]"
          }`}
        >
          {t.otpVerifySubtitle}{" "}
          <span className={`font-semibold ${isDark ? "text-white" : "text-[#111827]"}`}>
            {countryCode} {phoneNumber}
          </span>{" "}
          <button
            type="button"
            onClick={() => navigate(ROUTES.auth.otp)}
            className={`underline text-[12.5px] ml-1 cursor-pointer font-medium ${
              isDark ? "text-[#FFD98A] hover:text-amber-200" : "text-[#936639] hover:text-[#7F4F24]"
            }`}
          >
            ({t.changePhone})
          </button>
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

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* 6-Digit OTP Box Grid */}
        <div className="flex justify-between gap-2 py-2" dir="ltr">
          {digits.map((digit, idx) => (
            <input
              key={idx}
              ref={(el) => (inputRefs.current[idx] = el)}
              type="text"
              inputMode="numeric"
              maxLength={1}
              value={digit}
              onChange={(e) => handleDigitChange(idx, e.target.value)}
              onKeyDown={(e) => handleKeyDown(idx, e)}
              onPaste={handlePaste}
              className={`w-12 h-12 text-center text-lg font-semibold border rounded-lg outline-none transition-all ${
                isDark
                  ? "bg-[#161B22]/80 text-white border-white/10 focus:border-[#656D4A] focus:ring-2 focus:ring-[#656D4A]/30"
                  : "bg-white text-[#111827] border-[#E5E7EB] focus:border-[#A4AC86] focus:ring-2 focus:ring-[#A4AC86]/20"
              }`}
            />
          ))}
        </div>

        {/* Resend Timer Handler */}
        <div className="flex items-center justify-between text-[12.5px]">
          <span className={isDark ? "text-gray-400" : "text-[#6B7280]"}>{t.didNotReceive}</span>
          {timer.isActive ? (
            <span className={`font-mono ${isDark ? "text-gray-500" : "text-[#9CA3AF]"}`}>
              {t.resendIn} {timer.secondsLeft}s
            </span>
          ) : (
            <button
              type="button"
              onClick={handleResend}
              className={`font-medium transition-colors cursor-pointer ${
                isDark ? "text-[#FFD98A] hover:text-amber-200" : "text-[#936639] hover:text-[#7F4F24]"
              }`}
            >
              {t.resendNow}
            </button>
          )}
        </div>

        <ArchitecturalButton
          type="submit"
          isLoading={isLoading}
          loadingText={t.verifying}
          disabled={digits.some((d) => !d)}
          isDark={isDark}
        >
          {t.verifyCodeBtn}
        </ArchitecturalButton>
      </form>
    </div>
  );
}
