/**
 * Reset password form validation schema.
 * Pure validation logic — no UI state, no API calls.
 */

export interface ResetPasswordFormValues {
  newPassword: string;
  confirmPassword: string;
}

export interface ResetPasswordFormErrors {
  newPassword?: string;
  confirmPassword?: string;
}

export function validateResetPasswordForm(
  values: ResetPasswordFormValues,
): ResetPasswordFormErrors {
  const errors: ResetPasswordFormErrors = {};

  if (!values.newPassword) {
    errors.newPassword = "New password is required.";
  } else if (values.newPassword.length < 8) {
    errors.newPassword = "Password must be at least 8 characters.";
  }

  if (!values.confirmPassword) {
    errors.confirmPassword = "Please confirm your new password.";
  } else if (values.confirmPassword !== values.newPassword) {
    errors.confirmPassword = "Passwords do not match.";
  }

  return errors;
}

export function isResetPasswordFormValid(values: ResetPasswordFormValues): boolean {
  return Object.keys(validateResetPasswordForm(values)).length === 0;
}

/** Inline mismatch check — used for real-time field feedback in the UI. */
export function isPasswordMismatch(newPassword: string, confirmPassword: string): boolean {
  return confirmPassword.length > 0 && confirmPassword !== newPassword;
}
