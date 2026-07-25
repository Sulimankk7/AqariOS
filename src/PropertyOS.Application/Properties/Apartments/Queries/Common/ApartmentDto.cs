using System;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Apartments.Queries.Common;

public class ApartmentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid FloorId { get; set; }
    public string UnitNumber { get; set; } = null!;
    public OwnershipStatus OwnershipStatus { get; set; }
    public string? ExternalOwnerName { get; set; }
    public string? ExternalOwnerPhone { get; set; }
    public OccupancyStatus OccupancyStatus { get; set; }
    public decimal AreaSqm { get; set; }
    public short Bedrooms { get; set; }
    public short Bathrooms { get; set; }
    public decimal? BaseRentAmount { get; set; }
    public string BaseRentCurrency { get; set; } = "JOD";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
