using System.ComponentModel.DataAnnotations;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Api.Models.Properties;

public class CreateApartmentRequest
{
    [Required]
    [StringLength(20)]
    public string UnitNumber { get; set; } = null!;

    [Range(0.01, 10000.0)]
    public decimal AreaSqm { get; set; }

    public OwnershipStatus OwnershipStatus { get; set; } = OwnershipStatus.CompanyOwned;

    [StringLength(255)]
    public string? ExternalOwnerName { get; set; }

    [StringLength(20)]
    public string? ExternalOwnerPhone { get; set; }

    [Range(0, 100)]
    public short Bedrooms { get; set; } = 0;

    [Range(0, 100)]
    public short Bathrooms { get; set; } = 0;

    public decimal? BaseRentAmount { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string BaseRentCurrency { get; set; } = "JOD";
}
