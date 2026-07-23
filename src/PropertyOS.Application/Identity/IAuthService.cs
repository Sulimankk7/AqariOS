using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity;

/// <summary>
/// Service interface handling authentication operations (Login, Token Refresh, Revocation, OTP, User Profiles).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates user credentials and issues JWT access token + refresh token.
    /// </summary>
    /// <param name="dto">Login request payload.</param>
    /// <param name="ipAddress">Originating IP address.</param>
    /// <param name="userAgent">User agent string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response containing tokens and profile.</returns>
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token and issues a new access token.
    /// Performs reuse detection; if a revoked token is presented, revokes the entire token family.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token string.</param>
    /// <param name="ipAddress">Originating IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New login response containing rotated tokens.</returns>
    Task<LoginResponseDto> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token session on logout.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh token sessions for a specified user across all devices.
    /// </summary>
    /// <param name="userId">User unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and records an OTP challenge code for phone authentication.
    /// </summary>
    /// <param name="dto">OTP request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated OTP code string (for dev/SMS gateway dispatch).</returns>
    Task<string> RequestOtpAsync(OtpRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an OTP code and authenticates the user.
    /// </summary>
    /// <param name="dto">OTP verify payload.</param>
    /// <param name="ipAddress">Originating IP address.</param>
    /// <param name="userAgent">User agent string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response upon successful OTP verification.</returns>
    Task<LoginResponseDto> VerifyOtpAsync(
        OtpVerifyDto dto,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves user profile, company roles, and permission keys for authenticated user.
    /// </summary>
    /// <param name="userId">Authenticated user unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User profile DTO.</returns>
    Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
