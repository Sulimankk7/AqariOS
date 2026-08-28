using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Domain.Identity.Entities;

/// <summary>
/// Records the review lifecycle for a landlord account and its provisioned company.
/// </summary>
public sealed class LandlordRegistration
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid MembershipId { get; private set; }
    public RegistrationApprovalStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public UserCompanyRole Membership { get; private set; } = null!;
    public User? Reviewer { get; private set; }

    private LandlordRegistration()
    {
    }

    public static LandlordRegistration CreatePending(
        Guid userId,
        Guid companyId,
        Guid membershipId,
        DateTimeOffset submittedAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (companyId == Guid.Empty) throw new ArgumentException("Company ID is required.", nameof(companyId));
        if (membershipId == Guid.Empty) throw new ArgumentException("Membership ID is required.", nameof(membershipId));

        return new LandlordRegistration
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CompanyId = companyId,
            MembershipId = membershipId,
            Status = RegistrationApprovalStatus.Pending,
            SubmittedAt = submittedAt,
            CreatedAt = submittedAt,
            UpdatedAt = submittedAt
        };
    }

    public void Approve(Guid reviewedBy, DateTimeOffset reviewedAt)
    {
        EnsurePending();
        if (reviewedBy == Guid.Empty) throw new ArgumentException("Reviewer ID is required.", nameof(reviewedBy));

        Status = RegistrationApprovalStatus.Approved;
        RejectionReason = null;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        UpdatedAt = reviewedAt;
    }

    public void Reject(string reason, Guid reviewedBy, DateTimeOffset reviewedAt)
    {
        EnsurePending();
        if (reviewedBy == Guid.Empty) throw new ArgumentException("Reviewer ID is required.", nameof(reviewedBy));

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        if (normalizedReason.Length > 500)
            throw new ArgumentException("Rejection reason must not exceed 500 characters.", nameof(reason));

        Status = RegistrationApprovalStatus.Rejected;
        RejectionReason = normalizedReason;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        UpdatedAt = reviewedAt;
    }

    private void EnsurePending()
    {
        if (Status != RegistrationApprovalStatus.Pending)
            throw new InvalidOperationException("Only a pending landlord registration can be reviewed.");
    }
}
