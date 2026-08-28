using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Subscriptions.Security;

public static class SubscriptionsPermissions
{
    public const string PlansView = PlatformPermissions.SubscriptionPlansView;
    public const string OwnSubscriptionView = PlatformPermissions.OwnSubscriptionView;
    public const string OwnPlanChangeRequestsRead = PlatformPermissions.OwnPlanChangeRequestsRead;
    public const string OwnPlanChangeRequestsCreate = PlatformPermissions.OwnPlanChangeRequestsCreate;
    public const string OwnPlanChangeRequestsCancel = PlatformPermissions.OwnPlanChangeRequestsCancel;

    public const string PlatformPlansRead = PlatformPermissions.PlatformPlansRead;
    public const string PlatformPlansCreate = PlatformPermissions.PlatformPlansCreate;
    public const string PlatformPlansLifecycle = PlatformPermissions.PlatformPlansLifecycle;
    public const string PlatformSubscriptionsRead = PlatformPermissions.PlatformSubscriptionsRead;
    public const string PlatformSubscriptionsManage = PlatformPermissions.PlatformSubscriptionsManage;
    public const string PlatformPlanChangeRequestsRead = PlatformPermissions.PlatformPlanChangeRequestsRead;
    public const string PlatformPlanChangeRequestsReview = PlatformPermissions.PlatformPlanChangeRequestsReview;
}
