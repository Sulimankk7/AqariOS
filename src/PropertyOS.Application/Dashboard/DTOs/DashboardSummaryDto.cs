namespace PropertyOS.Application.Dashboard.DTOs;

/// <summary>
/// Root DTO representing full dashboard summary KPI metrics.
/// </summary>
public class DashboardSummaryDto
{
    public PropertySummaryDto Property { get; set; } = new();
    public LeasingSummaryDto Leasing { get; set; } = new();
    public PaymentSummaryDto Payments { get; set; } = new();
    public FinancialSummaryDto Financials { get; set; } = new();
}
