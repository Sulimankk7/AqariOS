using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Leasing;

/// <summary>
/// Represents a renter. Maps to the PostgreSQL table: tenants (Module 5 §1.9).
/// </summary>
public class Tenant : ISoftDeletable
{
    public Guid Id { get; private set; }
    
    /// <summary>UUID NOT NULL — tenant scope.</summary>
    public Guid CompanyId { get; private set; }

    /// <summary>VARCHAR(255) NOT NULL.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>VARCHAR(50) NOT NULL. Sensitive/redaction required.</summary>
    [Sensitive]
    public string NationalId { get; private set; } = string.Empty;

    /// <summary>VARCHAR(20) NOT NULL. Sensitive/redaction required.</summary>
    [Sensitive]
    public string Phone { get; private set; } = string.Empty;

    /// <summary>VARCHAR(100) NULL.</summary>
    public string? Occupation { get; private set; }

    /// <summary>VARCHAR(100) NULL.</summary>
    public string? Employer { get; private set; }

    /// <summary>UUID NULL REFERENCES users(id). Populated if portal activated.</summary>
    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private readonly List<TenantFamilyMember> _familyMembers = new();
    public IReadOnlyCollection<TenantFamilyMember> FamilyMembers => _familyMembers.AsReadOnly();

    private readonly List<TenantEmergencyContact> _emergencyContacts = new();
    public IReadOnlyCollection<TenantEmergencyContact> EmergencyContacts => _emergencyContacts.AsReadOnly();

    private readonly List<TenantVehicle> _vehicles = new();
    public IReadOnlyCollection<TenantVehicle> Vehicles => _vehicles.AsReadOnly();

    private Tenant() { }

    public static Tenant Create(
        Guid companyId,
        string name,
        string nationalId,
        string phone,
        DateTimeOffset createdAt,
        Guid? createdBy,
        string? occupation = null,
        string? employer = null,
        Guid? userId = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId is required.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(nationalId))
            throw new ArgumentException("NationalId is required.", nameof(nationalId));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));

        return new Tenant
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            Name = name.Trim(),
            NationalId = nationalId.Trim(),
            Phone = phone.Trim(),
            Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim(),
            Employer = string.IsNullOrWhiteSpace(employer) ? null : employer.Trim(),
            UserId = userId,
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
