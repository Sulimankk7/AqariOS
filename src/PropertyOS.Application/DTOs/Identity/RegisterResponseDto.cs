using System;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Response payload returned upon successful tenant registration and onboarding.
/// </summary>
public class RegisterResponseDto
{
    public Guid RegistrationId { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset SubmittedAt { get; set; }
    public string Message { get; set; } = null!;
}
