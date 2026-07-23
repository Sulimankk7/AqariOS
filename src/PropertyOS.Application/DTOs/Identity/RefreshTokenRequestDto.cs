namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Request payload for token refresh operations.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh token string. Optional if supplied via HttpOnly cookie.
    /// </summary>
    public string? RefreshToken { get; set; }
}
