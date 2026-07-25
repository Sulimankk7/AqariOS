using System;
using System.ComponentModel.DataAnnotations;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Api.Models.Properties;

public class CreateParkingSpotRequest
{
    [Required]
    [StringLength(50)]
    public string SpotCode { get; set; } = null!;

    public ParkingType ParkingType { get; set; } = ParkingType.Standard;

    public Guid? DefaultApartmentId { get; set; }

    [StringLength(255)]
    public string? LocationDescription { get; set; }
}
