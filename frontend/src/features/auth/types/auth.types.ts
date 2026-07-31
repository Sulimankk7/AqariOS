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
  companyId: string;
  companyName: string;
  role: string;
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
  userId: string;
  companyId: string;
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserProfileDto;
}

export interface LoginRequestDto {
  emailOrPhone: string;
  password: string;
}

export interface LoginResponseDto {
  accessToken: string;
  refreshToken: string;
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

// ── Application Core Auth State ───────────────────────────────────────────────

export interface AuthState {
  currentUser: UserProfileDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}
