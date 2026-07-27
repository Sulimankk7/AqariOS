using System;

namespace PropertyOS.Application.Leasing.Queries.GetTenantById;

public class TenantFamilyMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string? AgeBracket { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
