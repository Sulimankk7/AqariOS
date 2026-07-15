using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Leasing;

public class TenantVehicle : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid TenantId { get; private set; }

    public string PlateNumber { get; private set; } = string.Empty;
    public string MakeModel { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private TenantVehicle() { }

    public static TenantVehicle Create(
        Guid companyId,
        Guid tenantId,
        string plateNumber,
        string makeModel,
        string color,
        DateTimeOffset createdAt,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId required");
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required");
        if (string.IsNullOrWhiteSpace(plateNumber)) throw new ArgumentException("PlateNumber required");
        if (string.IsNullOrWhiteSpace(makeModel)) throw new ArgumentException("MakeModel required");
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color required");

        return new TenantVehicle
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            TenantId = tenantId,
            PlateNumber = plateNumber.Trim(),
            MakeModel = makeModel.Trim(),
            Color = color.Trim(),
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
