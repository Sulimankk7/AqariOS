using System;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Response payload issued upon successful authentication.
/// Contains the JWT access token and user profile data.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// JWT Access Token string.
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// Opaque refresh token string (used when HttpOnly cookie is unavailable or as payload fallback).
    /// </summary>
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// Token type string (e.g. "Bearer").
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
