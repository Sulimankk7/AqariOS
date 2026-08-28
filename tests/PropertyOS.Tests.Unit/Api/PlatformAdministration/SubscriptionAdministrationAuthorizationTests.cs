using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PropertyOS.Api.PlatformAdministration;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Subscriptions.Security;

namespace PropertyOS.Tests.Unit.Api.PlatformAdministration;

public sealed class SubscriptionAdministrationAuthorizationTests
{
    [Theory]
    [InlineData(typeof(PlansController))]
    [InlineData(typeof(SubscriptionsController))]
    [InlineData(typeof(PlanChangeRequestsController))]
    [InlineData(typeof(CompaniesController))]
    public void PlatformController_RequiresSystemAdminRole(Type controllerType)
    {
        var authorize = controllerType.GetCustomAttributes<AuthorizeAttribute>()
            .Single(x => x.Roles is not null);
        Assert.Equal(PlatformRoles.SystemAdmin, authorize.Roles);
    }

    [Fact]
    public void CompanySelection_RequiresSubscriptionManagementPermission()
    {
        var authorize = typeof(CompaniesController).GetMethod(nameof(CompaniesController.GetCompanies))!
            .GetCustomAttributes<AuthorizeAttribute>().Single(x => x.Policy is not null);

        Assert.Equal(SubscriptionsPermissions.PlatformSubscriptionsManage, authorize.Policy);
        Assert.True(PlatformPermissions.IsPlatformOnly(authorize.Policy!));
    }

    [Theory]
    [InlineData(typeof(PlansController), nameof(PlansController.CreatePlan), SubscriptionsPermissions.PlatformPlansCreate)]
    [InlineData(typeof(PlansController), nameof(PlansController.Deactivate), SubscriptionsPermissions.PlatformPlansLifecycle)]
    [InlineData(typeof(SubscriptionsController), nameof(SubscriptionsController.CreateSubscription), SubscriptionsPermissions.PlatformSubscriptionsManage)]
    [InlineData(typeof(PlanChangeRequestsController), nameof(PlanChangeRequestsController.Approve), SubscriptionsPermissions.PlatformPlanChangeRequestsReview)]
    [InlineData(typeof(PlanChangeRequestsController), nameof(PlanChangeRequestsController.Reject), SubscriptionsPermissions.PlatformPlanChangeRequestsReview)]
    public void PlatformMutation_RequiresExplicitPlatformPermission(Type controllerType, string methodName, string permission)
    {
        var authorize = controllerType.GetMethod(methodName)!
            .GetCustomAttributes<AuthorizeAttribute>().Single(x => x.Policy is not null);
        Assert.Equal(permission, authorize.Policy);
        Assert.True(PlatformPermissions.IsPlatformOnly(permission));
    }
}
