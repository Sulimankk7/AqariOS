using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Maintenance.Security;

/// <summary>
/// Module 8 Maintenance permission constants aliased to the central <see cref="PlatformPermissions"/> source of truth.
/// </summary>
public static class MaintenancePermissions
{
    public const string Create = PlatformPermissions.MaintenanceCreate;
    public const string UpdateStatus = PlatformPermissions.MaintenanceUpdateStatus;
    public const string Comment = PlatformPermissions.MaintenanceComment;
}
