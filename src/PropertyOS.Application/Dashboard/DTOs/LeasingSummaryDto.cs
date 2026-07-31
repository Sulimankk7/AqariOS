namespace PropertyOS.Application.Dashboard.DTOs;

/// <summary>
/// DTO representing leasing contract KPI summary metrics.
/// </summary>
public class LeasingSummaryDto
{
    public int ActiveLeases { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int NewLeasesThisMonth { get; set; }
}
