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

    /// <summary>VARCHAR(255) NULL. Contact and delivery email.</summary>
    [Sensitive]
    public string? Email { get; private set; }

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
        string? email = null,
        string? occupation = null,
        string? employer = null,
        Guid? userId = null,
        string? phoneCountryCode = "JO")
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
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            // The column default uuid_generate_v7() remains as a fallback for rows created
            // outside this factory.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Name = name.Trim(),
            NationalId = nationalId.Trim(),
            Phone = TenantPhoneNumber.Normalize(phone, phoneCountryCode),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim(),
            Employer = string.IsNullOrWhiteSpace(employer) ? null : employer.Trim(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    /// <summary>
    /// Updates the tenant's personal details. National-ID uniqueness within the company
    /// is an application/database concern (uq_tenants_company_national_id), not enforced here.
    /// </summary>
    public void UpdateDetails(
        string name,
        string nationalId,
        string phone,
        string? occupation,
        string? employer,
        DateTimeOffset updatedAt,
        Guid? updatedBy,
        string? email = null,
        string? phoneCountryCode = "JO")
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot update a deleted tenant.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(nationalId))
            throw new ArgumentException("NationalId is required.", nameof(nationalId));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));

        Name = name.Trim();
        NationalId = nationalId.Trim();
        Phone = TenantPhoneNumber.Normalize(phone, phoneCountryCode);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim();
        Employer = string.IsNullOrWhiteSpace(employer) ? null : employer.Trim();
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

    /// <summary>
    /// Links the tenant record to a User identity account.
    /// Throws InvalidOperationException if the tenant is already linked or deleted.
    /// </summary>
    public void LinkUser(Guid userId)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot link a user to a deleted tenant.");
        if (UserId.HasValue)
            throw new InvalidOperationException("Tenant account is already linked to a user.");
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
