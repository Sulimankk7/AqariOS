using System;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Floors.Queries.Common;

public class FloorDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BuildingId { get; set; }
    public short FloorNumber { get; set; }
    public string FloorLabel { get; set; } = null!;
    public FloorType FloorType { get; set; }
    public int ApartmentsCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
