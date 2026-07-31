namespace PropertyOS.Application.Dashboard.DTOs;

/// <summary>
/// DTO representing property portfolio KPI summary metrics.
/// </summary>
public class PropertySummaryDto
{
    public int TotalBuildings { get; set; }
    public int TotalApartments { get; set; }
    public int OccupiedApartments { get; set; }
    public int VacantApartments { get; set; }
    public double OccupancyRate { get; set; }
}
