namespace PropertyOS.Application.Dashboard.DTOs;

/// <summary>
/// DTO representing rent collection & payment KPI summary metrics.
/// </summary>
public class PaymentSummaryDto
{
    public decimal CollectedThisMonth { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int OverduePayments { get; set; }
}
