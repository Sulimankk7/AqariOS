namespace PropertyOS.Application.PlatformAdministration.DTOs;

public sealed class LandlordRegistrationReviewResultDto
{
    public Guid RegistrationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReviewedAt { get; set; }
}
