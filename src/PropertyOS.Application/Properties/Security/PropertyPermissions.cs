using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Properties.Security;

/// <summary>
/// Module 4 Properties permission constants aliased to the central <see cref="PlatformPermissions"/> source of truth.
/// </summary>
public static class PropertyPermissions
{
    public const string Read = PlatformPermissions.PropertiesRead;
    public const string Create = PlatformPermissions.PropertiesCreate;
    public const string Update = PlatformPermissions.PropertiesUpdate;
    public const string Delete = PlatformPermissions.PropertiesDelete;
    public const string Manage = PlatformPermissions.PropertiesManage;
}
