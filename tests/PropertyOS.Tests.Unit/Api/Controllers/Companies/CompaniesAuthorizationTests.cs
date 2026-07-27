using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Companies;
using PropertyOS.Api.Controllers;
using PropertyOS.Application.Companies.Security;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Companies;

public class CompaniesAuthorizationTests
{
    [Fact]
    public void CompaniesController_ShouldBeDecoratedWithAuthorizeAttribute()
    {
        var attribute = typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>();
        attribute.Should().NotBeNull("CompaniesController must be decorated with [Authorize]");
    }

    [Theory]
    [InlineData(typeof(CompaniesController), "UpdateCompany")]
    [InlineData(typeof(CompaniesController), "UpdateCompanySettings")]
    [InlineData(typeof(SubscriptionController), "Subscribe")]
    [InlineData(typeof(SubscriptionController), "ChangePlan")]
    [InlineData(typeof(SubscriptionController), "Cancel")]
    public void OrgLevelMutatingAction_ShouldRequireCompanyManagePolicy(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull($"Action {methodName} must exist on {controllerType.Name}");

        var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttr.Should().NotBeNull($"Action {controllerType.Name}.{methodName} must have an [Authorize] attribute");
        authorizeAttr!.Policy.Should().Be(
            CompaniesPermissions.Manage,
            $"Action {controllerType.Name}.{methodName} must require policy '{CompaniesPermissions.Manage}'");
    }

    [Fact]
    public void CompaniesController_ShouldOnlyExposeTheVersionedRoute()
    {
        var routes = typeof(CompaniesController)
            .GetCustomAttributes<RouteAttribute>()
            .Select(r => r.Template)
            .ToList();

        routes.Should().ContainSingle()
            .Which.Should().Be("api/v{version:apiVersion}/companies");
    }

    [Fact]
    public void CompaniesPermissions_Manage_ShouldAliasThePlatformCatalogKey()
    {
        CompaniesPermissions.Manage.Should().Be(
            PropertyOS.Application.Common.Security.PlatformPermissions.CompanyManage);
    }
}
