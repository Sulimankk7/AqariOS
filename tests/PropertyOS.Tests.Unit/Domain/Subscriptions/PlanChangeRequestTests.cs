using FluentAssertions;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Tests.Unit.Domain.Subscriptions;

public sealed class PlanChangeRequestTests
{
    [Fact]
    public void Create_PreservesSubmittedCommercialIntent()
    {
        var companyId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var currentPlanId = Guid.NewGuid();
        var requestedPlanId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var requestedAt = DateTimeOffset.UtcNow;

        var request = PlanChangeRequest.Create(
            companyId, subscriptionId, currentPlanId, requestedPlanId,
            BillingCycleEnum.Monthly, BillingCycleEnum.Yearly, requesterId, requestedAt);

        request.Status.Should().Be(PlanChangeRequestStatus.Pending);
        request.CompanyId.Should().Be(companyId);
        request.SubscriptionId.Should().Be(subscriptionId);
        request.CurrentPlanId.Should().Be(currentPlanId);
        request.RequestedPlanId.Should().Be(requestedPlanId);
        request.CurrentBillingCycle.Should().Be(BillingCycleEnum.Monthly);
        request.RequestedBillingCycle.Should().Be(BillingCycleEnum.Yearly);
        request.RequestedBy.Should().Be(requesterId);
        request.RequestedAt.Should().Be(requestedAt);
    }

    [Fact]
    public void Cancel_AllowsOnlyPendingRequest()
    {
        var request = CreatePending();
        request.Cancel();

        request.Status.Should().Be(PlanChangeRequestStatus.Cancelled);
        var act = () => request.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_RequiresReasonAndPreservesDecision()
    {
        var request = CreatePending();
        var reviewerId = Guid.NewGuid();
        var reviewedAt = DateTimeOffset.UtcNow;

        request.Reject(reviewerId, reviewedAt, " Requested Plan is no longer eligible. ", "Reviewed manually");

        request.Status.Should().Be(PlanChangeRequestStatus.Rejected);
        request.ReviewerId.Should().Be(reviewerId);
        request.ReviewedAt.Should().Be(reviewedAt);
        request.RejectionReason.Should().Be("Requested Plan is no longer eligible.");
        request.DecisionNote.Should().Be("Reviewed manually");
    }

    [Fact]
    public void Approve_CannotBeAppliedTwice()
    {
        var request = CreatePending();
        request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow, null);

        var act = () => request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        act.Should().Throw<InvalidOperationException>();
    }

    private static PlanChangeRequest CreatePending() => PlanChangeRequest.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        BillingCycleEnum.Monthly, BillingCycleEnum.Yearly, Guid.NewGuid(), DateTimeOffset.UtcNow);
}
