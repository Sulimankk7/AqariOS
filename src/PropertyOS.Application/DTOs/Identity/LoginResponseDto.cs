using System;
using System.Text.Json.Serialization;

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
    /// Opaque refresh token used by the API layer to issue the HttpOnly cookie.
    /// It is never serialized into browser-visible response bodies.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// Indicates whether the refresh cookie survives browser restarts.
    /// Safe session metadata used by the client to choose access-token storage.
    /// </summary>
    public bool IsPersistentSession { get; set; }

    /// <summary>
    /// Server-side refresh-token deadline used by the API layer when issuing
    /// a persistent cookie. It is not exposed to browser JavaScript.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

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
