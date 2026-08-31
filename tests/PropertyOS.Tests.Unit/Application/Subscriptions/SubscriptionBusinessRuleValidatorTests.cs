using FluentAssertions;
using PropertyOS.Application.Subscriptions.UseCases;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Tests.Unit.Application.Subscriptions;

public sealed class SubscriptionBusinessRuleValidatorTests
{
    [Fact]
    public void CreatePlan_RejectsInvalidCommercialConfiguration()
    {
        var validator = new CreatePlanCommandValidator();
        var command = new CreatePlanCommand(
            "PRO", "Pro", "احترافي", null, null,
            -1, 0, "jod", -1, 0, -5, "[]", true, null, -1);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(x => x.PropertyName).Should().Contain([
            "MonthlyPrice", "YearlyPrice", "Currency", "MaxBuildings", "MaxUsers",
            "MaxStorageMb", "FeatureFlags", "TrialDurationDays", "SortOrder"]);
    }

    [Fact]
    public void CreatePlan_AcceptsBooleanAndEnumStringFeatureFlags()
    {
        var validator = new CreatePlanCommandValidator();
        var command = new CreatePlanCommand(
            "PRO-2026", "Pro", "احترافي", null, null,
            25, 250, "JOD", null, null, null,
            "{\"reports\":true,\"support\":\"priority\"}", false, null, 10);

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePlan_FixedPricing_RemainsBackwardCompatible()
    {
        var command = new CreatePlanCommand(
            "FIXED", "Fixed", "ثابت", null, null,
            25, 250, "JOD", null, null, null, "{}", false, null, 0);

        new CreatePlanCommandValidator().Validate(command).IsValid.Should().BeTrue();
        command.PricingModel.Should().Be(SubscriptionPricingModel.Fixed);
    }

    [Fact]
    public void CreatePlan_PaygRequiresExplicitUnitRatesAndDoesNotOverloadFixedPrices()
    {
        var valid = new CreatePlanCommand(
            "PAYG", "PAYG", "حسب الاستخدام", null, null,
            0, 0, "JOD", null, null, null, "{}", false, null, 0,
            SubscriptionPricingModel.PayAsYouGo, 3m, 2m);

        new CreatePlanCommandValidator().Validate(valid).IsValid.Should().BeTrue();

        var missingRates = valid with { PaygMonthlyUnitPrice = null };
        new CreatePlanCommandValidator().Validate(missingRates).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejection_RequiresMeaningfulReason()
    {
        var validator = new RejectPlanChangeRequestCommandValidator();
        var result = validator.Validate(new RejectPlanChangeRequestCommand(Guid.NewGuid(), " ", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "RejectionReason");
    }
}
