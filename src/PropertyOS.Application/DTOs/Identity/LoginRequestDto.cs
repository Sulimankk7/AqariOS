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
}
