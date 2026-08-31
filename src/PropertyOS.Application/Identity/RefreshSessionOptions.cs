namespace PropertyOS.Application.Identity;

/// <summary>Configures server-side refresh-session lifetimes.</summary>
public sealed class RefreshSessionOptions
{
    public const string SectionName = "RefreshSession";

    /// <summary>
    /// Finite server-side lifetime for ordinary and non-password-login sessions.
    /// </summary>
    public int NormalLifetimeDays { get; set; } = 7;

    /// <summary>
    /// Absolute lifetime for password-login sessions that opt into Remember Me.
    /// </summary>
    public int RememberedLifetimeDays { get; set; } = 30;
}
