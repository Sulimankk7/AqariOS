namespace PropertyOS.Api.Models.Leasing;

public class UpdateTenantFamilyMemberRequest
{
    public string Name { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string? AgeBracket { get; set; }
}
