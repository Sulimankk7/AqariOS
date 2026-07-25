using System;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Response payload returned upon successful tenant registration and onboarding.
/// </summary>
public class RegisterResponseDto
{
    /// <summary>
    /// Created User unique identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Created Company/Tenant unique identifier.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// JWT Access Token string.
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// Opaque refresh token string.
    /// </summary>
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// Token type (e.g. "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Access token validity duration in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User profile and authorization details.
    /// </summary>
    public UserProfileDto User { get; set; } = null!;
}
