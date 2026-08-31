namespace PropertyOS.Application.Notifications.Options;

/// <summary>
/// Configuration settings for Infobip SMS API integration.
/// </summary>
public sealed class InfobipOptions
{
    public const string SectionName = "Infobip";

    /// <summary>
    /// Infobip API key. Configure only through local secrets or Infobip__ApiKey.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Account-specific Infobip API base URL, for example https://{account}.api.infobip.com.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Configured Infobip sender ID. This value must be authorized for the account.
    /// </summary>
    public string Sender { get; set; } = string.Empty;
}
