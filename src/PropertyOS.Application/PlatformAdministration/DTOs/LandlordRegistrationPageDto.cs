namespace PropertyOS.Application.PlatformAdministration.DTOs;

public sealed class LandlordRegistrationPageDto
{
    public IReadOnlyList<LandlordRegistrationListItemDto> Items { get; set; } = Array.Empty<LandlordRegistrationListItemDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
