using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PropertyOS.Api.UtilityBills;
using PropertyOS.Application.UtilityBills.Security;
using PropertyOS.Application.Common.Security;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.UtilityBills;

public sealed class UtilityBillsControllerTests
{
    [Fact]
    public void ManagementController_UsesCanonicalRouteAndManagementPolicy()
    {
        typeof(UtilityAccountsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/v{version:apiVersion}/utility-bills/accounts");

        foreach (var method in typeof(UtilityAccountsController)
                     .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            method.GetCustomAttribute<AuthorizeAttribute>()!.Policy
                .Should().Be(UtilityBillsPermissions.Manage);
        }
    }

    [Fact]
    public void ManagementController_ExposesOnlyCanonicalRequiredEndpoints()
    {
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.List))
            .Should().Be("GET:");
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.Link))
            .Should().Be("POST:");
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.Replace))
            .Should().Be("POST:{id:guid}/replace");
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.Unlink))
            .Should().Be("DELETE:{id:guid}");
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.GetById))
            .Should().Be("GET:{id:guid}");
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.GetBills))
            .Should().Be("GET:{id:guid}/bills");
        Endpoint(typeof(UtilityAccountsController), nameof(UtilityAccountsController.Sync))
            .Should().Be("POST:{id:guid}/sync");
    }

    [Fact]
    public void TenantController_UsesCanonicalMyRouteAndOwnDataPolicy()
    {
        var controller = typeof(TenantPortalUtilityBillsController);
        controller.GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/v{version:apiVersion}/utility-bills/my");

        var authorize = controller.GetCustomAttribute<AuthorizeAttribute>()!;
        authorize.Roles.Should().Be("TENANT");
        authorize.Policy.Should().Be(PlatformPermissions.TenantPortalAccess);

        Endpoint(controller, nameof(TenantPortalUtilityBillsController.GetMyUtilityAccounts))
            .Should().Be("GET:accounts");
        Endpoint(controller, nameof(TenantPortalUtilityBillsController.LinkMyUtilityAccount))
            .Should().Be("POST:accounts");
        Endpoint(controller, nameof(TenantPortalUtilityBillsController.ReplaceMyUtilityAccount))
            .Should().Be("POST:accounts/{id:guid}/replace");
        Endpoint(controller, nameof(TenantPortalUtilityBillsController.UnlinkMyUtilityAccount))
            .Should().Be("DELETE:accounts/{id:guid}");
        Endpoint(controller, nameof(TenantPortalUtilityBillsController.SyncMyUtilityAccount))
            .Should().Be("POST:accounts/{id:guid}/sync");
        Endpoint(controller, nameof(TenantPortalUtilityBillsController.GetMyUtilityBills))
            .Should().Be("GET:bills");
    }

    private static string Endpoint(Type controller, string methodName)
    {
        var method = controller.GetMethod(methodName)!;
        var http = method.GetCustomAttributes<HttpMethodAttribute>().Single();
        return $"{http.HttpMethods.Single()}:{http.Template}";
    }
}
