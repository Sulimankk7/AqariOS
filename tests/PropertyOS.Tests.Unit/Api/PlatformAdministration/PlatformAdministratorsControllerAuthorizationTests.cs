using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PropertyOS.Api.PlatformAdministration;
using PropertyOS.Api.PlatformAdministration.Requests;
using PropertyOS.Application.Common.Security;

namespace PropertyOS.Tests.Unit.Api.PlatformAdministration;

public sealed class PlatformAdministratorsControllerAuthorizationTests
{
    [Fact]
    public void Controller_RequiresSystemAdminRole()
    {
        var authorize = typeof(PlatformAdministratorsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Roles is not null);

        Assert.Equal(PlatformRoles.SystemAdmin, authorize.Roles);
    }

    [Fact]
    public void CreateRequest_ExposesOnlyNameEmailAndPassword()
    {
        var properties = typeof(CreatePlatformAdministratorRequest)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(new[] { "Email", "FullName", "Password" }, properties);
    }
}
