namespace PropertyOS.Application.UtilityBills.Security;

/// <summary>
/// Authorization permission constants for the Utility Bills module.
/// Follows the existing AqariOS permission naming convention.
/// </summary>
public static class UtilityBillsPermissions
{
    /// <summary>
    /// Permission required to link or unlink utility accounts on a lease.
    /// Intended for company admins / property managers.
    /// </summary>
    public const string Manage = "UtilityBills.Manage";

}
