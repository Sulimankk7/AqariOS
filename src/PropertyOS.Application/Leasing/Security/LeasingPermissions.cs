using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Leasing.Security;

/// <summary>
/// Module 5 Leasing permission constants aliased to the central <see cref="PlatformPermissions"/> source of truth.
/// </summary>
public static class LeasingPermissions
{
    public const string Create = PlatformPermissions.ContractsCreate;
    public const string Approve = PlatformPermissions.ContractsApprove;
}
