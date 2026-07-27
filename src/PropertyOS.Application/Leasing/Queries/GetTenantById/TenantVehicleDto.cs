using System;

namespace PropertyOS.Application.Leasing.Queries.GetTenantById;

public class TenantVehicleDto
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string MakeModel { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
