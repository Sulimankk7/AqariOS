/**
 * Registration form validation schema.
 * Pure validation logic — no UI state, no API calls.
 * Replace with zod/yup schemas when added as dependencies.
 */

export interface RegisterFormValues {
  fullName: string;
  companyName: string;
  displayName: string;
  companyType: string;
  email: string;
  phoneCountryCode: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
  preferredLanguage: string;
  agreedToTerms: boolean;
}

export interface RegisterFormErrors {
  fullName?: string;
  companyName?: string;
  companyType?: string;
  email?: string;
  phoneNumber?: string;
  password?: string;
  confirmPassword?: string;
  agreedToTerms?: string;
}

export function validateRegisterForm(values: RegisterFormValues): RegisterFormErrors {
  const errors: RegisterFormErrors = {};

  if (!values.fullName.trim()) {
    errors.fullName = "Full name is required.";
  }

  if (!values.companyName.trim()) {
    errors.companyName = "Company name is required.";
  }

  if (!values.companyType) {
    errors.companyType = "Please select a company type.";
  }

  if (!values.email) {
    errors.email = "Email address is required.";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) {
    errors.email = "Please enter a valid email address.";
  }

  if (!values.phoneNumber.trim()) {
    errors.phoneNumber = "Phone number is required.";
  }

  if (!values.password) {
    errors.password = "Password is required.";
  } else if (values.password.length < 8) {
    errors.password = "Password must be at least 8 characters.";
  }

  if (!values.confirmPassword) {
    errors.confirmPassword = "Please confirm your password.";
  } else if (values.confirmPassword !== values.password) {
    errors.confirmPassword = "Passwords do not match.";
  }

  if (!values.agreedToTerms) {
    errors.agreedToTerms = "You must agree to the Terms of Service and Privacy Policy.";
  }

  return errors;
}

export function isRegisterFormValid(values: RegisterFormValues): boolean {
  return Object.keys(validateRegisterForm(values)).length === 0;
}
