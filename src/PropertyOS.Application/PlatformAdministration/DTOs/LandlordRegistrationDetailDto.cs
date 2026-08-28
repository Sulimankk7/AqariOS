namespace PropertyOS.Application.PlatformAdministration.DTOs;

public sealed class LandlordRegistrationDetailDto
{
    public Guid RegistrationId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyDisplayName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public DateTimeOffset RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}
