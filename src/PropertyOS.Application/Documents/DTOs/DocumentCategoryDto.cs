using System;

namespace PropertyOS.Application.Documents.DTOs;

public record DocumentCategoryDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt
);
