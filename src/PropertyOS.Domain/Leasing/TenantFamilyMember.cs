using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Leasing;

public class TenantFamilyMember : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    
    public string RelationshipType { get; private set; } = string.Empty;
    
    public string? AgeBracket { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private TenantFamilyMember() { }

    public static TenantFamilyMember Create(
        Guid companyId,
        Guid tenantId,
        string name,
        string relationshipType,
        DateTimeOffset createdAt,
        Guid? createdBy,
        string? ageBracket = null)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId required");
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required");
        if (string.IsNullOrWhiteSpace(relationshipType)) throw new ArgumentException("RelationshipType required");

        return new TenantFamilyMember
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            TenantId = tenantId,
            Name = name.Trim(),
            RelationshipType = relationshipType.Trim(),
            AgeBracket = string.IsNullOrWhiteSpace(ageBracket) ? null : ageBracket.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
