namespace PropertyOS.Application.Common.Options;

/// <summary>
/// Configuration settings for the frontend web application URLs.
/// </summary>
public class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// Base URL of the frontend web application (default: http://localhost:5173).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5173";
}
