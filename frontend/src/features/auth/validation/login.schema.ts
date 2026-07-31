/**
 * Login form validation schema.
 * Pure validation logic — no UI state, no API calls.
 * Replace with zod/yup schemas when added as dependencies.
 */

export interface LoginFormValues {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface LoginFormErrors {
  email?: string;
  password?: string;
}

export function validateLoginForm(values: LoginFormValues): LoginFormErrors {
  const errors: LoginFormErrors = {};

  if (!values.email) {
    errors.email = "Email address is required.";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) {
    errors.email = "Please enter a valid email address.";
  }

  if (!values.password) {
    errors.password = "Password is required.";
  }

  return errors;
}

export function isLoginFormValid(values: LoginFormValues): boolean {
  return Object.keys(validateLoginForm(values)).length === 0;
}
