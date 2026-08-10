using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Leasing;

public class TenantEmergencyContact : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string RelationshipType { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private TenantEmergencyContact() { }

    public static TenantEmergencyContact Create(
        Guid companyId,
        Guid tenantId,
        string name,
        string relationshipType,
        string phone,
        DateTimeOffset createdAt,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId required");
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required");
        if (string.IsNullOrWhiteSpace(relationshipType)) throw new ArgumentException("RelationshipType required");
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone required");

        return new TenantEmergencyContact
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            TenantId = tenantId,
            Name = name.Trim(),
            RelationshipType = relationshipType.Trim(),
            Phone = phone.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateDetails(
        string name,
        string relationshipType,
        string phone,
        DateTimeOffset updatedAt,
        Guid? updatedBy)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot update a deleted emergency contact.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(relationshipType))
            throw new ArgumentException("RelationshipType is required.", nameof(relationshipType));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));

        Name = name.Trim();
        RelationshipType = relationshipType.Trim();
        Phone = phone.Trim();
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
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
