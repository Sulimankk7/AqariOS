/**
 * OTP form validation schemas (Phone OTP request + 6-digit verification).
 * Pure validation logic — no UI state, no API calls.
 */

// ── Phone OTP Request ─────────────────────────────────────────────────────────

export interface PhoneOtpFormValues {
  phoneCountryCode: string;
  phoneNumber: string;
}

export interface PhoneOtpFormErrors {
  phoneNumber?: string;
}

export function validatePhoneOtpForm(values: PhoneOtpFormValues): PhoneOtpFormErrors {
  const errors: PhoneOtpFormErrors = {};

  if (!values.phoneNumber.trim()) {
    errors.phoneNumber = "Phone number is required.";
  }

  return errors;
}

// ── OTP Verification ──────────────────────────────────────────────────────────

export interface VerifyOtpFormValues {
  /** Array of single-character digit strings, length 6. */
  digits: string[];
}

export interface VerifyOtpFormErrors {
  code?: string;
}

export function validateVerifyOtpForm(values: VerifyOtpFormValues): VerifyOtpFormErrors {
  const errors: VerifyOtpFormErrors = {};
  const code = values.digits.join("");

  if (code.length < 6) {
    errors.code = "Please enter the full 6-digit verification code.";
  } else if (!/^\d{6}$/.test(code)) {
    errors.code = "Verification code must contain only digits.";
  }

  return errors;
}

export function isVerifyOtpFormComplete(digits: string[]): boolean {
  return digits.every((d) => /^\d$/.test(d));
}
