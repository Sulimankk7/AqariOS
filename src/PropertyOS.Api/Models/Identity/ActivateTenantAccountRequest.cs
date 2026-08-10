namespace PropertyOS.Api.Models.Identity;

/// <summary>
/// Payload parameters for tenant account activation and initial password setup.
/// </summary>
public class ActivateTenantAccountRequest
{
    /// <summary>
    /// Cryptographically secure one-time activation token.
    /// </summary>
    public string ActivationToken { get; set; } = string.Empty;

    /// <summary>
    /// Initial password for the tenant user account.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
