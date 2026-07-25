using System.ComponentModel.DataAnnotations;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Api.Models.Properties;

public class UpdateBuildingRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    public BuildingType BuildingType { get; set; }

    [StringLength(20)]
    public string? InternalCode { get; set; }

    public short? ConstructionYear { get; set; }

    [Range(-90.0, 90.0)]
    public decimal? GpsLatitude { get; set; }

    [Range(-180.0, 180.0)]
    public decimal? GpsLongitude { get; set; }

    public Governorate AddressGovernorate { get; set; }

    [Required]
    [StringLength(50)]
    public string AddressCity { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string AddressNeighborhood { get; set; } = null!;

    [StringLength(100)]
    public string? AddressStreet { get; set; }

    [StringLength(20)]
    public string? AddressPostalCode { get; set; }
}
