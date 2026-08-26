using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Financials.Security;

/// <summary>
/// Modules 6–7 Financials permission constants aliased to the central <see cref="PlatformPermissions"/> source of truth.
/// </summary>
public static class FinancialsPermissions
{
    public const string PaymentsRead = PlatformPermissions.PaymentsRead;
    public const string PaymentsApprove = PlatformPermissions.PaymentsApprove;
    public const string ChequesRead = PlatformPermissions.ChequesRead;
    public const string ReceiptsRead = PlatformPermissions.ReceiptsRead;
    public const string ExpensesCreate = PlatformPermissions.ExpensesCreate;
    public const string ExpensesApprove = PlatformPermissions.ExpensesApprove;
    public const string ReceiptsIssue = PlatformPermissions.ReceiptsIssue;
}
