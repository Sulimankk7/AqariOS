namespace PropertyOS.Api.Models.Leasing;

public class CreateTenantVehicleRequest
{
    public string PlateNumber { get; set; } = string.Empty;
    public string MakeModel { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
