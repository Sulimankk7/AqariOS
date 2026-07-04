using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Domain.Properties;

public class ParkingSpot : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BuildingId { get; private set; }
    public Guid? DefaultApartmentId { get; private set; }
    
    public string SpotCode { get; private set; } = null!;
    public ParkingType ParkingType { get; private set; }
    public string? LocationDescription { get; private set; }
    
    public bool IsActive { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private ParkingSpot() { }

    public static ParkingSpot Create(
        Guid companyId,
        Guid buildingId,
        string spotCode,
        DateTimeOffset createdAt,
        Guid? createdBy,
        ParkingType parkingType = ParkingType.Standard,
        Guid? defaultApartmentId = null,
        string? locationDescription = null)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("Required", nameof(companyId));
        if (buildingId == Guid.Empty) throw new ArgumentException("Required", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(spotCode)) throw new ArgumentException("Required", nameof(spotCode));

        return new ParkingSpot
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            BuildingId = buildingId,
            DefaultApartmentId = defaultApartmentId,
            SpotCode = spotCode.Trim(),
            ParkingType = parkingType,
            LocationDescription = string.IsNullOrWhiteSpace(locationDescription) ? null : locationDescription.Trim(),
            IsActive = true,
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
        IsActive = false;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
