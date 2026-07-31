/**
 * Forgot password form validation schema.
 * Pure validation logic — no UI state, no API calls.
 */

export type RecoveryMethod = "email" | "phone";

export interface ForgotPasswordFormValues {
  method: RecoveryMethod;
  contact: string;
}

export interface ForgotPasswordFormErrors {
  contact?: string;
}

export function validateForgotPasswordForm(
  values: ForgotPasswordFormValues,
): ForgotPasswordFormErrors {
  const errors: ForgotPasswordFormErrors = {};

  if (!values.contact.trim()) {
    errors.contact =
      values.method === "email"
        ? "Email address is required."
        : "Phone number is required.";
    return errors;
  }

  if (values.method === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.contact)) {
    errors.contact = "Please enter a valid email address.";
  }

  return errors;
}

export function isForgotPasswordFormValid(values: ForgotPasswordFormValues): boolean {
  return Object.keys(validateForgotPasswordForm(values)).length === 0;
}
