using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Domain.Subscriptions;

public class PlanChangeRequest
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid CurrentPlanId { get; private set; }
    public Guid RequestedPlanId { get; private set; }
    public BillingCycleEnum CurrentBillingCycle { get; private set; }
    public BillingCycleEnum RequestedBillingCycle { get; private set; }
    public Guid RequestedBy { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public PlanChangeRequestStatus Status { get; private set; }
    public Guid? ReviewerId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? DecisionNote { get; private set; }
    public string? RejectionReason { get; private set; }

    public Company Company { get; private set; } = null!;
    public CompanySubscription Subscription { get; private set; } = null!;
    public SubscriptionPlan CurrentPlan { get; private set; } = null!;
    public SubscriptionPlan RequestedPlan { get; private set; } = null!;
    public User Requester { get; private set; } = null!;
    public User? Reviewer { get; private set; }

    private PlanChangeRequest()
    {
    }

    public static PlanChangeRequest Create(
        Guid companyId,
        Guid subscriptionId,
        Guid currentPlanId,
        Guid requestedPlanId,
        BillingCycleEnum currentBillingCycle,
        BillingCycleEnum requestedBillingCycle,
        Guid requestedBy,
        DateTimeOffset requestedAt)
    {
        if (companyId == Guid.Empty || subscriptionId == Guid.Empty ||
            currentPlanId == Guid.Empty || requestedPlanId == Guid.Empty ||
            requestedBy == Guid.Empty)
            throw new ArgumentException("Plan change request identifiers must not be empty.");

        return new PlanChangeRequest
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            SubscriptionId = subscriptionId,
            CurrentPlanId = currentPlanId,
            RequestedPlanId = requestedPlanId,
            CurrentBillingCycle = currentBillingCycle,
            RequestedBillingCycle = requestedBillingCycle,
            RequestedBy = requestedBy,
            RequestedAt = requestedAt,
            Status = PlanChangeRequestStatus.Pending
        };
    }

    public void Cancel()
    {
        EnsurePending("cancelled");
        Status = PlanChangeRequestStatus.Cancelled;
    }

    public void Approve(Guid reviewerId, DateTimeOffset reviewedAt, string? decisionNote)
    {
        EnsurePending("approved");
        if (reviewerId == Guid.Empty)
            throw new ArgumentException("Reviewer ID is required.", nameof(reviewerId));

        Status = PlanChangeRequestStatus.Approved;
        ReviewerId = reviewerId;
        ReviewedAt = reviewedAt;
        DecisionNote = NormalizeOptional(decisionNote);
        RejectionReason = null;
    }

    public void Reject(Guid reviewerId, DateTimeOffset reviewedAt, string rejectionReason, string? decisionNote)
    {
        EnsurePending("rejected");
        if (reviewerId == Guid.Empty)
            throw new ArgumentException("Reviewer ID is required.", nameof(reviewerId));
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason is required.", nameof(rejectionReason));

        Status = PlanChangeRequestStatus.Rejected;
        ReviewerId = reviewerId;
        ReviewedAt = reviewedAt;
        DecisionNote = NormalizeOptional(decisionNote);
        RejectionReason = rejectionReason.Trim();
    }

    private void EnsurePending(string action)
    {
        if (Status != PlanChangeRequestStatus.Pending)
            throw new InvalidOperationException($"Only a pending plan change request can be {action}.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
