using System.Text.Json.Serialization;

namespace PropertyOS.Application.DTOs.Identity;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PasswordResetDeliveryMethod
{
    Email,
    Phone
}

public sealed class PasswordResetRequestDto
{
    public PasswordResetDeliveryMethod DeliveryMethod { get; set; }
    public string Identifier { get; set; } = string.Empty;
}

public sealed class PasswordResetRequestResponseDto
{
    public string Message { get; init; } = "If an eligible account exists, recovery instructions have been sent.";
}

public sealed class PasswordResetOtpVerifyDto
{
    public string Phone { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class PasswordResetOtpVerifyResponseDto
{
    public string ResetAuthorization { get; init; } = string.Empty;
    public int ExpiresInSeconds { get; init; }
}

public sealed class PasswordResetCompleteDto
{
    public string ResetCredential { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class PasswordResetCompleteResponseDto
{
    public string Message { get; init; } = "Password reset completed successfully. Please sign in.";
}
