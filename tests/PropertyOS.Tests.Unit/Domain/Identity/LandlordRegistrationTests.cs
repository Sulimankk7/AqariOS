using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Tests.Unit.Domain.Identity;

public sealed class LandlordRegistrationTests
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 8, 27, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreatePending_CapturesProvisionedAccountAndStartsPending()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        var registration = LandlordRegistration.CreatePending(userId, companyId, membershipId, SubmittedAt);

        Assert.NotEqual(Guid.Empty, registration.Id);
        Assert.Equal(userId, registration.UserId);
        Assert.Equal(companyId, registration.CompanyId);
        Assert.Equal(membershipId, registration.MembershipId);
        Assert.Equal(RegistrationApprovalStatus.Pending, registration.Status);
        Assert.Equal(SubmittedAt, registration.SubmittedAt);
        Assert.Null(registration.ReviewedAt);
        Assert.Null(registration.ReviewedBy);
    }

    [Fact]
    public void Approve_TransitionsPendingRegistrationExactlyOnce()
    {
        var registration = CreatePending();
        var reviewerId = Guid.NewGuid();
        var reviewedAt = SubmittedAt.AddHours(1);

        registration.Approve(reviewerId, reviewedAt);

        Assert.Equal(RegistrationApprovalStatus.Approved, registration.Status);
        Assert.Equal(reviewerId, registration.ReviewedBy);
        Assert.Equal(reviewedAt, registration.ReviewedAt);
        Assert.Throws<InvalidOperationException>(() => registration.Approve(reviewerId, reviewedAt));
        Assert.Throws<InvalidOperationException>(() => registration.Reject("late rejection", reviewerId, reviewedAt));
    }

    [Fact]
    public void Reject_NormalizesReasonAndTransitionsExactlyOnce()
    {
        var registration = CreatePending();
        var reviewerId = Guid.NewGuid();
        var reviewedAt = SubmittedAt.AddHours(1);

        registration.Reject("  Missing ownership document.  ", reviewerId, reviewedAt);

        Assert.Equal(RegistrationApprovalStatus.Rejected, registration.Status);
        Assert.Equal("Missing ownership document.", registration.RejectionReason);
        Assert.Equal(reviewerId, registration.ReviewedBy);
        Assert.Equal(reviewedAt, registration.ReviewedAt);
        Assert.Throws<InvalidOperationException>(() => registration.Reject("again", reviewerId, reviewedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_RequiresAReason(string reason)
    {
        var registration = CreatePending();

        Assert.Throws<ArgumentException>(() =>
            registration.Reject(reason, Guid.NewGuid(), SubmittedAt.AddHours(1)));
    }

    [Fact]
    public void Reject_RejectsReasonLongerThanDatabaseLimit()
    {
        var registration = CreatePending();

        Assert.Throws<ArgumentException>(() =>
            registration.Reject(new string('x', 501), Guid.NewGuid(), SubmittedAt.AddHours(1)));
    }

    private static LandlordRegistration CreatePending() =>
        LandlordRegistration.CreatePending(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SubmittedAt);
}
