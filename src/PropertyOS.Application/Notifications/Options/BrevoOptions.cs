namespace PropertyOS.Application.Notifications.Options;

/// <summary>
/// Configuration settings for Brevo transactional email integration.
/// </summary>
public class BrevoOptions
{
    public const string SectionName = "Brevo";

    /// <summary>
    /// Brevo v3 API secret key supplied via environment or configuration.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Verified sender email address (default: aqari.system@gmail.com).
    /// </summary>
    public string SenderEmail { get; set; } = "aqari.system@gmail.com";

    /// <summary>
    /// Display name of the sender (default: عقاري نوت).
    /// </summary>
    public string SenderName { get; set; } = "عقاري نوت";
}
