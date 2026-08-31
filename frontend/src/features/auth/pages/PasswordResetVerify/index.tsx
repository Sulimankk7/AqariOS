import React, { useEffect, useState } from "react";
import { useLocation, useNavigate, useOutletContext } from "react-router";
import { AlertCircle, ShieldCheck } from "lucide-react";
import { authApi } from "@/features/auth/api/auth.api";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { inputClass } from "@/features/auth/components/FieldLabel";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { useOtpTimer } from "@/features/auth/hooks/useOtpTimer";
import { ApiError } from "@/shared/lib/http";
import { extractUserFriendlyError } from "@/shared/utils/errorHandling";
import { ROUTES } from "@/config/routes";

export default function PasswordResetVerifyPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const context = useOutletContext<{ lang?: "en" | "ar"; isDark?: boolean }>();
  const lang = context?.lang ?? "ar";
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];
  const phone = (location.state as { phone?: string } | null)?.phone ?? "";
  const [code, setCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const timer = useOtpTimer(60);

  useEffect(() => { timer.start(); }, []);

  const verify = async (event: React.FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    if (!phone || !/^\d{6}$/.test(code)) return;
    setIsLoading(true);
    try {
      const result = await authApi.verifyPasswordResetOtp({ phone, code });
      navigate(ROUTES.auth.resetPassword, {
        replace: true,
        state: { resetAuthorization: result.resetAuthorization },
      });
    } catch (error) {
      setErrorMessage(extractUserFriendlyError(error));
    } finally {
      setIsLoading(false);
    }
  };

  const resend = async () => {
    setErrorMessage(null);
    try {
      await authApi.requestPasswordReset({ deliveryMethod: "Phone", identifier: phone });
      timer.start(60);
    } catch (error) {
      if (error instanceof ApiError && error.retryAfterSeconds) timer.start(error.retryAfterSeconds);
      setErrorMessage(extractUserFriendlyError(error));
    }
  };

  if (!phone) {
    return (
      <div className="space-y-5 text-center">
        <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
        <p>{t.resetCodeInvalidAccess}</p>
        <ArchitecturalButton type="button" onClick={() => navigate(ROUTES.auth.forgotPassword)} isDark={isDark}>
          {t.forgotPassword}
        </ArchitecturalButton>
      </div>
    );
  }

  return (
    <div>
      <ShieldCheck className="mb-4 h-10 w-10 text-primary" />
      <h1 className="mb-1 text-2xl font-semibold">{t.resetCodeTitle}</h1>
      <p className="mb-5 text-sm text-muted-foreground">{t.resetCodeSubtitle}</p>
      {errorMessage && <div role="alert" className="mb-4 flex gap-2 rounded-lg border border-red-300 bg-red-50 p-3 text-xs text-red-700"><AlertCircle size={15} />{errorMessage}</div>}
      <form onSubmit={verify} className="space-y-4">
        <input value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, "").slice(0, 6))}
          inputMode="numeric" autoComplete="one-time-code" maxLength={6} className={`${inputClass} text-center text-xl tracking-[0.5em]`} />
        <ArchitecturalButton type="submit" disabled={code.length !== 6} isLoading={isLoading} loadingText={t.verifying} isDark={isDark}>
          {t.verifyCodeBtn}
        </ArchitecturalButton>
      </form>
      <button type="button" disabled={timer.isActive} onClick={resend} className="mt-4 text-xs text-primary disabled:text-muted-foreground">
        {timer.isActive ? `${t.resendIn} ${timer.secondsLeft}s` : t.resendNow}
      </button>
    </div>
  );
}
