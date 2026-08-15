using System;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Response payload for preflight tenant activation token validation.
/// </summary>
public class TenantActivationStatusDto
{
    /// <summary>
    /// Explicit token state: "VALID", "EXPIRED", "ALREADY_USED", "INVALID".
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Localized, safe human-friendly message for presentation.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// The tenant's full name (only populated when Status is VALID).
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// When the activation token expires (only populated when available).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
