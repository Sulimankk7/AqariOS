using System.ComponentModel.DataAnnotations;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Api.Models.Properties;

public class UpdateFloorRequest
{
    [Required]
    [StringLength(50)]
    public string FloorLabel { get; set; } = null!;

    public FloorType FloorType { get; set; }
}
