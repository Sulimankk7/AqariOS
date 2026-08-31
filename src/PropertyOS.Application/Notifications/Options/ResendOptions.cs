namespace PropertyOS.Application.Notifications.Options;

/// <summary>
/// Configuration settings for the Resend transactional email integration.
/// </summary>
public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>
    /// Resend API key. Configure through local secrets or the Resend__ApiKey
    /// environment variable; never commit the value.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Sender address whose domain is verified in Resend.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Friendly sender display name.
    /// </summary>
    public string SenderName { get; set; } = "Aqari System";
}
