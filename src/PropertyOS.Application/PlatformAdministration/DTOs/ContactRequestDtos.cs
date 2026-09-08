namespace PropertyOS.Application.PlatformAdministration.DTOs;

public sealed record ContactRequestCreatedDto(Guid Id, string Status, DateTimeOffset CreatedAt);
public sealed record ContactRequestListItemDto(Guid Id, string Name, string CompanyName, string PhoneNumber, int NumberOfBuildings, string Status, DateTimeOffset CreatedAt);
public sealed record ContactRequestDetailDto(Guid Id, string Name, string CompanyName, string PhoneNumber, int NumberOfBuildings, string? Notes, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ContactRequestPageDto(IReadOnlyList<ContactRequestListItemDto> Items, int Page, int PageSize, int TotalCount);
