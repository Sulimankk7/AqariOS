using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Notifications.Security;

/// <summary>
/// Module 11 Notifications permission constants aliased to the central <see cref="PlatformPermissions"/> source of truth.
/// </summary>
public static class NotificationsPermissions
{
    public const string Send = PlatformPermissions.NotificationsSend;
    public const string ManageTemplates = PlatformPermissions.NotificationsManageTemplates;
    public const string ViewAll = PlatformPermissions.NotificationsViewAll;
}
