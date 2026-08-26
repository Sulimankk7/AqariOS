using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PropertyOS.Api.Financials;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;
using PropertyOS.Application.Financials.Commands.RejectPaymentSubmission;
using PropertyOS.Application.Financials.Security;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Financials;

public class OwnerPaymentsControllerTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        var controllerType = typeof(OwnerPaymentsController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData("GetPendingVerifications")]
    [InlineData("ApproveSubmission")]
    [InlineData("RejectSubmission")]
    public void Endpoints_AreProtectedWithPaymentsApprovePolicy(string methodName)
    {
        var method = typeof(OwnerPaymentsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == methodName);

        method.Should().NotBeNull($"Method {methodName} must exist on OwnerPaymentsController");

        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttribute.Should().NotBeNull($"Method {methodName} must be decorated with [Authorize]");
        authorizeAttribute!.Policy.Should().Be(PlatformPermissions.PaymentsApprove,
            $"Method {methodName} must require the '{PlatformPermissions.PaymentsApprove}' authorization policy");
    }

    [Fact]
    public async Task ApproveSubmission_DispatchesApprovePaymentSubmissionCommand()
    {
        // Arrange
        var sender = Substitute.For<ISender>();
        var controller = new OwnerPaymentsController(sender);
        var paymentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        // Act
        var result = await controller.ApproveSubmission(paymentId, submissionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await sender.Received(1).Send(
            Arg.Is<ApprovePaymentSubmissionCommand>(c => c.SubmissionId == submissionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectSubmission_DispatchesRejectPaymentSubmissionCommand()
    {
        // Arrange
        var sender = Substitute.For<ISender>();
        var controller = new OwnerPaymentsController(sender);
        var paymentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var request = new RejectPaymentSubmissionRequest("Invalid transfer reference number");

        // Act
        var result = await controller.RejectSubmission(paymentId, submissionId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await sender.Received(1).Send(
            Arg.Is<RejectPaymentSubmissionCommand>(c => c.SubmissionId == submissionId && c.Reason == "Invalid transfer reference number"),
            Arg.Any<CancellationToken>());
    }
}
