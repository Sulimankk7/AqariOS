using System;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
namespace PropertyOS.Application.Properties.Buildings.Queries.Common;

public class BuildingAddressDto
{
    public Guid BuildingId { get; set; }
    public Governorate Governorate { get; set; }
    public string District { get; set; } = null!;
    public string? Area { get; set; }
    public string? StreetName { get; set; }
    public string? PostalCode { get; set; }
}

public class BuildingDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? InternalCode { get; set; }
    public BuildingType BuildingType { get; set; }
    public short TotalFloors { get; set; }
    public short? ConstructionYear { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public int TotalApartmentsCount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public BuildingAddressDto? Address { get; set; }
}
