import React, { useState } from "react";
import { useLocation, useNavigate, useOutletContext, useSearchParams } from "react-router";
import { KeyRound, Lock, Eye, EyeOff, AlertCircle } from "lucide-react";
import { FieldLabel, inputClass } from "@/features/auth/components/FieldLabel";
import { ArchitecturalButton } from "@/features/auth/components/ArchitecturalButton";
import { TRANSLATIONS } from "@/features/auth/constants/translations";
import { authApi } from "@/features/auth/api/auth.api";
import { validateResetPasswordForm } from "@/features/auth/validation/resetPassword.schema";
import { extractUserFriendlyError, localizeValidationMessage } from "@/shared/utils/errorHandling";
import { ApiError } from "@/shared/lib/http";
import { ROUTES } from "@/config/routes";

interface ResetPasswordFormProps {
  lang: "en" | "ar";
  onFeedbackMessage?: (msg: string) => void;
}

export function ResetPasswordForm({ lang, onFeedbackMessage }: ResetPasswordFormProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const context = useOutletContext<{ isDark?: boolean }>();
  const isDark = context?.isDark ?? false;
  const t = TRANSLATIONS[lang];
  const state = location.state as { resetAuthorization?: string } | null;
  const resetCredential = searchParams.get("token")?.trim() || state?.resetAuthorization?.trim() || "";

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<{ newPassword?: string; confirmPassword?: string }>({});

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    const errors = validateResetPasswordForm({ newPassword, confirmPassword });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0 || !resetCredential) return;

    setIsLoading(true);
    try {
      await authApi.completePasswordReset({ resetCredential, newPassword });
      if (onFeedbackMessage) onFeedbackMessage(t.passwordResetSuccess);
      navigate(ROUTES.auth.login, { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        const passwordMessages = error.validationErrors?.NewPassword ?? error.validationErrors?.newPassword;
        if (passwordMessages?.[0]) {
          setFieldErrors((current) => ({
            ...current,
            newPassword: localizeValidationMessage(passwordMessages[0]),
          }));
        }
      }
      setErrorMessage(extractUserFriendlyError(error));
    } finally {
      setIsLoading(false);
    }
  };

  if (!resetCredential) {
    return (
      <div className="space-y-5 text-center">
        <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
        <h1 className="text-xl font-semibold">{t.resetCredentialMissing}</h1>
        <ArchitecturalButton type="button" onClick={() => navigate(ROUTES.auth.forgotPassword)} isDark={isDark}>
          {t.forgotPassword}
        </ArchitecturalButton>
      </div>
    );
  }

  return (
    <div>
      <h1 className="mb-1 text-2xl font-semibold">{t.resetTitle}</h1>
      <p className="mb-1 text-sm text-muted-foreground">{t.resetSubtitle}</p>
      <p className="mb-5 text-xs text-muted-foreground">{t.passwordPolicy}</p>

      {errorMessage && <div role="alert" className="mb-4 flex gap-2 rounded-lg border border-red-300 bg-red-50 p-3 text-xs text-red-700"><AlertCircle size={15} />{errorMessage}</div>}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <FieldLabel isDark={isDark}>{t.newPasswordLabel}</FieldLabel>
          <div className="relative">
            <KeyRound size={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input type={showPassword ? "text" : "password"} required value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)} className={`${inputClass} pl-9 pr-10`} />
            <button type="button" onClick={() => setShowPassword((value) => !value)}
              aria-label={t.togglePasswordVisibility} className="absolute right-3 top-1/2 -translate-y-1/2">
              {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          {fieldErrors.newPassword && <p className="mt-1 text-xs text-red-500">{localizeValidationMessage(fieldErrors.newPassword)}</p>}
        </div>

        <div>
          <FieldLabel isDark={isDark}>{t.confirmPasswordLabel}</FieldLabel>
          <div className="relative">
            <Lock size={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input type="password" required value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)} className={`${inputClass} pl-9`} />
          </div>
          {fieldErrors.confirmPassword && <p className="mt-1 text-xs text-red-500">{localizeValidationMessage(fieldErrors.confirmPassword)}</p>}
        </div>

        <ArchitecturalButton type="submit" isLoading={isLoading} loadingText={t.updating} isDark={isDark}>
          {t.saveNewPasswordBtn}
        </ArchitecturalButton>
      </form>
    </div>
  );
}
