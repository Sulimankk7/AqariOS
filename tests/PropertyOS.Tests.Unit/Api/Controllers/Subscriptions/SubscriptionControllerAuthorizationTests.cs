using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PropertyOS.Api.Controllers;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Subscriptions.Security;

namespace PropertyOS.Tests.Unit.Api.Controllers.Subscriptions;

public sealed class SubscriptionControllerAuthorizationTests
{
    [Fact]
    public void Controller_RequiresCompanyAdminRole()
    {
        var authorize = typeof(SubscriptionController).GetCustomAttributes<AuthorizeAttribute>()
            .Single(x => x.Roles is not null);
        Assert.Equal(CompanyRoles.CompanyAdmin, authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(SubscriptionController.GetActivePlans), SubscriptionsPermissions.PlansView)]
    [InlineData(nameof(SubscriptionController.GetMySubscription), SubscriptionsPermissions.OwnSubscriptionView)]
    [InlineData(nameof(SubscriptionController.CreatePlanChangeRequest), SubscriptionsPermissions.OwnPlanChangeRequestsCreate)]
    [InlineData(nameof(SubscriptionController.CancelPlanChangeRequest), SubscriptionsPermissions.OwnPlanChangeRequestsCancel)]
    public void Endpoint_RequiresExplicitCompanyPermission(string methodName, string permission)
    {
        var authorize = typeof(SubscriptionController).GetMethod(methodName)!
            .GetCustomAttributes<AuthorizeAttribute>().Single(x => x.Policy is not null);
        Assert.Equal(permission, authorize.Policy);
        Assert.False(PlatformPermissions.IsPlatformOnly(permission));
    }
}
