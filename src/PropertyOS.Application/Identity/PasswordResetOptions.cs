namespace PropertyOS.Application.Identity;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int EmailTokenExpiryMinutes { get; set; } = 30;
    public int SmsOtpExpiryMinutes { get; set; } = 5;
    public int SmsAuthorizationExpiryMinutes { get; set; } = 10;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxRequestsPerDestinationWindow { get; set; } = 5;
    public int DestinationWindowMinutes { get; set; } = 60;
    public short MaxOtpAttempts { get; set; } = 3;
    public int MinimumRequestDurationMilliseconds { get; set; } = 300;
}
