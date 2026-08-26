using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using PropertyOS.Api.Financials;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Financials.Security;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Financials;

public class ChequesControllerTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var controllerType = typeof(ChequesController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData("GetCheques")]
    [InlineData("GetUpcoming")]
    public void ChequeReadEndpoints_AreProtectedWithChequesReadPolicy(string methodName)
    {
        var method = typeof(ChequesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Method {methodName} must exist on ChequesController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(FinancialsPermissions.ChequesRead,
            $"Method {methodName} must require the '{FinancialsPermissions.ChequesRead}' ({PlatformPermissions.ChequesRead}) authorization policy");
    }

    [Fact]
    public void RecordStatusChange_IsProtectedWithPaymentsApprovePolicy()
    {
        var method = typeof(ChequesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "RecordStatusChange");

        method.Should().NotBeNull("Method RecordStatusChange must exist on ChequesController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull("Method RecordStatusChange must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(FinancialsPermissions.PaymentsApprove,
            $"Method RecordStatusChange must require the '{FinancialsPermissions.PaymentsApprove}' authorization policy");
    }
}
