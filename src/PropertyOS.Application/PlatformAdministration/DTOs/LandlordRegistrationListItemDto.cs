namespace PropertyOS.Application.PlatformAdministration.DTOs;

public sealed class LandlordRegistrationListItemDto
{
    public Guid RegistrationId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTimeOffset RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
