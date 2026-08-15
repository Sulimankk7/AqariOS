namespace PropertyOS.Application.Notifications.Options;

/// <summary>
/// Configuration settings for Twilio SMS integration.
/// </summary>
public class TwilioOptions
{
    public const string SectionName = "Twilio";

    /// <summary>
    /// Twilio Account SID.
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token. Never log or expose this value.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Messaging Service SID (default: MGa02c1d790deba589a8ff733e39faeb37).
    /// </summary>
    public string MessagingServiceSid { get; set; } = "MGa02c1d790deba589a8ff733e39faeb37";
}
