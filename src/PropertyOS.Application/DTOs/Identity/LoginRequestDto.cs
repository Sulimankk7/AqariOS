namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Request payload for authentication login.
/// Supports login via Email or Jordanian Phone number (+962 format).
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// User email address or phone number.
    /// </summary>
    public string EmailOrPhone { get; set; } = null!;

    /// <summary>
    /// User account password.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// When true, requests a persistent refresh session with the configured
    /// remembered-session lifetime. False creates a browser-session cookie.
    /// Null is retained only for internal/legacy callers that historically
    /// received the normal persistent session.
    /// </summary>
    public bool? RememberMe { get; set; }
}
