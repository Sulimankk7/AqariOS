using System;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.ParkingSpots.Queries.Common;

public class ParkingSpotDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? DefaultApartmentId { get; set; }
    public string SpotCode { get; set; } = null!;
    public ParkingType ParkingType { get; set; }
    public string? LocationDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
