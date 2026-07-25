using System.ComponentModel.DataAnnotations;

namespace PropertyOS.Api.Models.Properties;

public class UpdateApartmentRequest
{
    public decimal? BaseRentAmount { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string BaseRentCurrency { get; set; } = "JOD";
}
