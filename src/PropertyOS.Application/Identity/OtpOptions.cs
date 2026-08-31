namespace PropertyOS.Application.Identity;

/// <summary>Configuration for the security-sensitive OTP workflow.</summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>Secret used as the HMAC key for persisted OTP hashes.</summary>
    public string HashKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxRequestsPerDestinationWindow { get; set; } = 5;
    public int DestinationWindowMinutes { get; set; } = 60;
}
