using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PropertyOS.Api.PlatformAdministration;
using PropertyOS.Application.Common.Security;

namespace PropertyOS.Tests.Unit.Api.PlatformAdministration;

public sealed class LandlordRegistrationsControllerAuthorizationTests
{
    [Fact]
    public void Controller_RequiresExplicitSystemAdminRole()
    {
        var authorize = typeof(LandlordRegistrationsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Roles is not null);

        Assert.Equal(PlatformRoles.SystemAdmin, authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(LandlordRegistrationsController.GetPending), PlatformPermissions.LandlordRegistrationsRead)]
    [InlineData(nameof(LandlordRegistrationsController.GetById), PlatformPermissions.LandlordRegistrationsRead)]
    [InlineData(nameof(LandlordRegistrationsController.Approve), PlatformPermissions.LandlordRegistrationsApprove)]
    [InlineData(nameof(LandlordRegistrationsController.Reject), PlatformPermissions.LandlordRegistrationsReject)]
    public void Endpoint_RequiresItsPlatformPermission(string methodName, string permission)
    {
        var method = typeof(LandlordRegistrationsController).GetMethod(methodName);
        var authorize = method!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy is not null);

        Assert.Equal(permission, authorize.Policy);
        Assert.True(PlatformPermissions.IsPlatformOnly(permission));
    }
}
