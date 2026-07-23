using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.DTOs.Subscriptions.Validators;
using PropertyOS.Application.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Subscriptions;

public class SubscriptionServiceUnitTests
{
    private readonly ISubscriptionService _subscriptionServiceMock;

    public SubscriptionServiceUnitTests()
    {
        _subscriptionServiceMock = Substitute.For<ISubscriptionService>();
    }

    [Fact]
    public void CreateSubscriptionRequestDtoValidator_Should_Pass_For_Valid_Data()
    {
        // Arrange
        var validator = new CreateSubscriptionRequestDtoValidator();
        var dto = new CreateSubscriptionRequestDto
        {
            PlanId = Guid.NewGuid(),
            BillingCycle = BillingCycleEnum.Monthly
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateSubscriptionRequestDtoValidator_Should_Fail_For_Empty_PlanId()
    {
        // Arrange
        var validator = new CreateSubscriptionRequestDtoValidator();
        var dto = new CreateSubscriptionRequestDto
        {
            PlanId = Guid.Empty,
            BillingCycle = BillingCycleEnum.Monthly
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "PlanId");
    }

    [Fact]
    public void ChangePlanRequestDtoValidator_Should_Pass_For_Valid_Data()
    {
        // Arrange
        var validator = new ChangePlanRequestDtoValidator();
        var dto = new ChangePlanRequestDto
        {
            NewPlanId = Guid.NewGuid(),
            NewBillingCycle = BillingCycleEnum.Yearly
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangePlanRequestDtoValidator_Should_Fail_For_Empty_NewPlanId()
    {
        // Arrange
        var validator = new ChangePlanRequestDtoValidator();
        var dto = new ChangePlanRequestDto
        {
            NewPlanId = Guid.Empty,
            NewBillingCycle = BillingCycleEnum.Yearly
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "NewPlanId");
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_Should_Return_Subscription_From_Service()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedDto = new UserSubscriptionDto
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PlanCode = "PRO",
            PlanNameEn = "Pro Plan",
            PlanNameAr = "خطة المحترفين",
            Status = "Active",
            BillingCycle = "Monthly",
            PriceAtSubscription = 49.99m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))
        };

        _subscriptionServiceMock
            .GetUserSubscriptionAsync(userId, Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        var result = await _subscriptionServiceMock.GetUserSubscriptionAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanCode.Should().Be("PRO");
        result.PriceAtSubscription.Should().Be(49.99m);
    }
}
