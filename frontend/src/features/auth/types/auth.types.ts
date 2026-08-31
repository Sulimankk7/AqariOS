/**
 * Authentication feature — TypeScript types.
 * Standardized DTO interface definitions matching the ASP.NET Core backend API contracts.
 */

// ── Backend Enums ─────────────────────────────────────────────────────────────

/**
 * CompanyType enum matching ASP.NET Core PropertyOS.Domain.Companies.Enums.CompanyType
 * 0 = IndividualOwner
 * 1 = PropertyManagementCompany
 * 2 = InvestmentCompany
 */
export enum CompanyType {
  IndividualOwner = 0,
  PropertyManagementCompany = 1,
  InvestmentCompany = 2,
}

// ── Backend DTOs ─────────────────────────────────────────────────────────────

export interface UserCompanyRoleDto {
  id: string;
  companyId: string;
  roleId: string;
  roleCode: string;
  roleName: string;
  status: string;
}

export interface UserProfileDto {
  id: string;
  email?: string;
  phone?: string;
  fullName: string;
  preferredLanguage: string;
  mfaEnabled: boolean;
  activeCompanyId?: string;
  companyRoles: UserCompanyRoleDto[];
  permissions: string[];
  systemRoles: string[];
}

export interface RegisterRequestDto {
  fullName: string;
  email?: string;
  phone?: string;
  password: string;
  companyName: string;
  displayName?: string;
  companyType?: CompanyType;
  countryCode?: string;
  preferredLanguage?: string;
}

export interface RegisterResponseDto {
  registrationId: string;
  status: string;
  submittedAt: string;
  message: string;
}

export interface LoginRequestDto {
  emailOrPhone: string;
  password: string;
  rememberMe: boolean;
}

export interface LoginResponseDto {
  accessToken: string;
  isPersistentSession: boolean;
  tokenType: string;
  expiresIn: number;
  user: UserProfileDto;
}

export interface OtpRequestDto {
  phone: string;
  purpose?: number; // 0 = Login
}

export interface OtpVerifyDto {
  phone: string;
  code: string;
  purpose?: number; // 0 = Login
}

export type PasswordResetDeliveryMethod = "Email" | "Phone";

export interface PasswordResetRequestDto {
  deliveryMethod: PasswordResetDeliveryMethod;
  identifier: string;
}

export interface PasswordResetRequestResponseDto {
  message: string;
}

export interface PasswordResetOtpVerifyDto {
  phone: string;
  code: string;
}

export interface PasswordResetOtpVerifyResponseDto {
  resetAuthorization: string;
  expiresInSeconds: number;
}

export interface PasswordResetCompleteDto {
  resetCredential: string;
  newPassword: string;
}

export interface PasswordResetCompleteResponseDto {
  message: string;
}

// ── Application Core Auth State ───────────────────────────────────────────────

export interface ActivateTenantAccountRequestDto {
  activationToken: string;
  password: string;
}

export interface TenantActivationStatusDto {
  status: 'VALID' | 'EXPIRED' | 'ALREADY_USED' | 'INVALID' | 'NOT_FOUND';
  message: string;
  tenantName?: string;
  expiresAt?: string;
}

export interface AuthState {
  currentUser: UserProfileDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}
