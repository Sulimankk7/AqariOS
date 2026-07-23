using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Request payload to verify an OTP challenge code.
/// </summary>
public class OtpVerifyDto
{
    /// <summary>
    /// Recipient phone number (+962 format).
    /// </summary>
    public string Phone { get; set; } = null!;

    /// <summary>
    /// 6-digit OTP challenge verification code.
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Purpose of the OTP challenge.
    /// </summary>
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
}
