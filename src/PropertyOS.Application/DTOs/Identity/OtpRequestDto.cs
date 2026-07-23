using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Request payload to initiate an OTP challenge.
/// </summary>
public class OtpRequestDto
{
    /// <summary>
    /// Recipient phone number (+962 format).
    /// </summary>
    public string Phone { get; set; } = null!;

    /// <summary>
    /// Purpose of the OTP challenge.
    /// </summary>
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
}
