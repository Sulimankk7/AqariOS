using FluentAssertions;
using PropertyOS.Application.Subscriptions.UseCases;

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
    public void Rejection_RequiresMeaningfulReason()
    {
        var validator = new RejectPlanChangeRequestCommandValidator();
        var result = validator.Validate(new RejectPlanChangeRequestCommand(Guid.NewGuid(), " ", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "RejectionReason");
    }
}
