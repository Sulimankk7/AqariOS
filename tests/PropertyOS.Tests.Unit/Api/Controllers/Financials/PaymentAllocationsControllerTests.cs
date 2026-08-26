using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using PropertyOS.Api.Financials;
using PropertyOS.Application.Financials.Security;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Financials;

public class PaymentAllocationsControllerTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var controllerType = typeof(PaymentAllocationsController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Record", FinancialsPermissions.PaymentsApprove)]
    [InlineData("Reverse", FinancialsPermissions.PaymentsApprove)]
    public void Endpoints_AreProtectedWithPaymentsApprovePolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(PaymentAllocationsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Method {methodName} must exist on PaymentAllocationsController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(expectedPolicy,
            $"Method {methodName} must require the '{expectedPolicy}' authorization policy");
    }
}
