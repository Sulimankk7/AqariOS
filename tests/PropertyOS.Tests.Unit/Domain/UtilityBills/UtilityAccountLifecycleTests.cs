using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.UtilityBills;

public sealed class UtilityAccountLifecycleTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Creator = Guid.NewGuid();

    [Fact]
    public void SoftDelete_ThenExactReactivate_PreservesIdentityAndHistoryState()
    {
        var account = Create("1234567890");
        account.MarkBootstrapCompleted(new DateOnly(2026, 7, 1), null, 0, null, CreatedAt.AddDays(1), CreatedAt.AddDays(30));
        var id = account.Id;
        var deletedAt = CreatedAt.AddDays(2);
        account.SoftDelete(deletedAt, Creator);

        account.Reactivate("1234567890", "M-2", deletedAt.AddHours(1), Creator);

        Assert.Equal(id, account.Id);
        Assert.True(account.IsActive);
        Assert.Null(account.DeletedAt);
        Assert.Equal(new DateOnly(2026, 7, 1), account.LastKnownBillDate);
        Assert.True(account.HistoricalBootstrapCompleted);
        Assert.Equal("M-2", account.MeterNumber);
        Assert.Equal(UtilitySyncStatus.NeverSynced, account.SyncStatus);
    }

    [Fact]
    public void Reactivate_DifferentAccountNumber_IsRejected()
    {
        var account = Create("1234567890");
        account.SoftDelete(CreatedAt.AddDays(1), Creator);
        Assert.Throws<InvalidOperationException>(() => account.Reactivate("0987654321", null, CreatedAt.AddDays(2), Creator));
    }

    [Fact]
    public void RefreshLinkDetails_DoesNotPermitChangingSubscriptionIdentity()
    {
        var account = Create("1234567890");
        Assert.Throws<InvalidOperationException>(() => account.RefreshLinkDetails("0987654321", null, CreatedAt.AddDays(1), Creator));
    }

    private static UtilityAccount Create(string number) => UtilityAccount.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        UtilityType.Electricity, number, null, CreatedAt, Creator);
}
