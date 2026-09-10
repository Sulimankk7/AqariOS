namespace PropertyOS.Application.PlatformAdministration.DTOs;

public sealed record PlatformAdministratorDto(
    Guid Id,
    string FullName,
    string? Email,
    bool IsActive,
    DateTimeOffset CreatedAt);
