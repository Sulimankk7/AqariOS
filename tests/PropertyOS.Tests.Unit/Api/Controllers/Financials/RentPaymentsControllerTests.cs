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

public class RentPaymentsControllerTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var controllerType = typeof(RentPaymentsController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData("GetById")]
    [InlineData("GetForLease")]
    [InlineData("GetForTenant")]
    [InlineData("Search")]
    [InlineData("GetOutstanding")]
    [InlineData("Get")]
    public void RentPaymentReadEndpoints_AreProtectedWithPaymentsReadPolicy(string methodName)
    {
        var method = typeof(RentPaymentsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Method {methodName} must exist on RentPaymentsController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(FinancialsPermissions.PaymentsRead,
            $"Method {methodName} must require the '{FinancialsPermissions.PaymentsRead}' ({PlatformPermissions.PaymentsRead}) authorization policy");
    }

    [Theory]
    [InlineData("GetReceipt")]
    [InlineData("GetReceipts")]
    public void ReceiptReadEndpoints_AreProtectedWithReceiptsReadPolicy(string methodName)
    {
        var method = typeof(RentPaymentsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Method {methodName} must exist on RentPaymentsController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(FinancialsPermissions.ReceiptsRead,
            $"Method {methodName} must require the '{FinancialsPermissions.ReceiptsRead}' ({PlatformPermissions.ReceiptsRead}) authorization policy");
    }

    [Theory]
    [InlineData("RecordManual", FinancialsPermissions.PaymentsApprove)]
    [InlineData("Cancel", FinancialsPermissions.PaymentsApprove)]
    [InlineData("GenerateInstallments", FinancialsPermissions.PaymentsApprove)]
    [InlineData("RemindTenant", FinancialsPermissions.PaymentsApprove)]
    [InlineData("IssueReceipt", FinancialsPermissions.ReceiptsIssue)]
    public void WriteEndpoints_PreserveExpectedPolicies(string methodName, string expectedPolicy)
    {
        var method = typeof(RentPaymentsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Method {methodName} must exist on RentPaymentsController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(expectedPolicy,
            $"Method {methodName} must require the '{expectedPolicy}' authorization policy");
    }
}
