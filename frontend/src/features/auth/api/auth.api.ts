/**
 * Authentication API Service.
 * Implements endpoints matching the ASP.NET Core backend controller:
 *   - POST /api/v1/auth/login
 *   - POST /api/v1/auth/register
 *   - POST /api/v1/auth/otp/request
 *   - POST /api/v1/auth/otp/verify
 *   - POST /api/v1/auth/refresh
 *   - GET /api/v1/auth/me
 */

import { http } from "@/shared/lib/http";
import type {
  LoginRequestDto,
  LoginResponseDto,
  RegisterRequestDto,
  RegisterResponseDto,
  OtpRequestDto,
  OtpVerifyDto,
  UserProfileDto,
  ActivateTenantAccountRequestDto,
  TenantActivationStatusDto,
  PasswordResetRequestDto,
  PasswordResetRequestResponseDto,
  PasswordResetOtpVerifyDto,
  PasswordResetOtpVerifyResponseDto,
  PasswordResetCompleteDto,
  PasswordResetCompleteResponseDto,
} from "@/features/auth/types/auth.types";

export const authApi = {
  /**
   * Authenticates user credentials (email/phone + password).
   * POST /api/v1/auth/login
   */
  login(dto: LoginRequestDto): Promise<LoginResponseDto> {
    return http.post<LoginResponseDto>("/api/v1/auth/login", dto);
  },

  /**
   * Registers a new user organization workspace.
   * POST /api/v1/auth/register
   */
  register(dto: RegisterRequestDto): Promise<RegisterResponseDto> {
    return http.post<RegisterResponseDto>("/api/v1/auth/register", dto);
  },

  /**
   * Requests an OTP verification challenge code.
   * POST /api/v1/auth/otp/request
   */
  requestOtp(dto: OtpRequestDto): Promise<{ message: string }> {
    return http.post<{ message: string }>("/api/v1/auth/otp/request", dto);
  },

  /**
   * Verifies an OTP code and authenticates the user.
   * POST /api/v1/auth/otp/verify
   */
  verifyOtp(dto: OtpVerifyDto): Promise<LoginResponseDto> {
    return http.post<LoginResponseDto>("/api/v1/auth/otp/verify", dto);
  },

  requestPasswordReset(dto: PasswordResetRequestDto): Promise<PasswordResetRequestResponseDto> {
    return http.post<PasswordResetRequestResponseDto>("/api/v1/auth/password-reset/request", dto, { skipAuth: true });
  },

  verifyPasswordResetOtp(dto: PasswordResetOtpVerifyDto): Promise<PasswordResetOtpVerifyResponseDto> {
    return http.post<PasswordResetOtpVerifyResponseDto>("/api/v1/auth/password-reset/verify-otp", dto, { skipAuth: true });
  },

  completePasswordReset(dto: PasswordResetCompleteDto): Promise<PasswordResetCompleteResponseDto> {
    return http.post<PasswordResetCompleteResponseDto>("/api/v1/auth/password-reset/complete", dto, { skipAuth: true });
  },

  /**
   * Silent token refresh using HttpOnly cookie.
   * POST /api/v1/auth/refresh
   */
  refreshToken(): Promise<LoginResponseDto> {
    return http.post<LoginResponseDto>("/api/v1/auth/refresh", {});
  },

  /** Revokes the current server-side refresh session and clears its cookie. */
  logout(): Promise<void> {
    return http.post<void>("/api/v1/auth/logout", {});
  },

  /**
   * Gets user profile context for authenticated session.
   * GET /api/v1/auth/me
   */
  getProfile(): Promise<UserProfileDto> {
    return http.get<UserProfileDto>("/api/v1/auth/me");
  },

  /**
   * Preflight validation of tenant activation token state.
   * GET /api/v1/auth/tenant-activation-status?token=...
   */
  getTenantActivationStatus(token: string): Promise<TenantActivationStatusDto> {
    return http.get<TenantActivationStatusDto>(`/api/v1/auth/tenant-activation-status?token=${encodeURIComponent(token)}`);
  },

  /**
   * Activates a tenant account using activation token and sets initial password.
   * POST /api/v1/auth/tenant-activate
   */
  activateTenant(dto: ActivateTenantAccountRequestDto): Promise<LoginResponseDto> {
    return http.post<LoginResponseDto>("/api/v1/auth/tenant-activate", dto);
  },
};
