using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.RequestUtilityAccountSync;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public sealed class RequestUtilityAccountSyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_BootstrappedAccount_RegistersIncrementalSyncOnlyAfterCommit()
    {
        var fixture = CreateFixture();
        var account = CreateAccount(fixture.CompanyId);
        account.MarkBootstrapCompleted(null, null, 0, null, fixture.Now, fixture.Now);
        fixture.Repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        await fixture.Handler.Handle(new RequestUtilityAccountSyncCommand(account.Id), CancellationToken.None);

        await fixture.Scheduler.DidNotReceiveWithAnyArgs().EnqueueSyncAsync(default, default);
        Assert.Single(fixture.Actions);
        await fixture.Actions[0](CancellationToken.None);
        await fixture.Scheduler.Received(1).EnqueueSyncAsync(account.Id, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_UnbootstrappedAccount_RegistersBootstrapOnlyAfterCommit()
    {
        var fixture = CreateFixture();
        var account = CreateAccount(fixture.CompanyId);
        fixture.Repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        await fixture.Handler.Handle(new RequestUtilityAccountSyncCommand(account.Id), CancellationToken.None);

        await fixture.Scheduler.DidNotReceiveWithAnyArgs().EnqueueBootstrapAsync(default, default);
        Assert.Single(fixture.Actions);
        await fixture.Actions[0](CancellationToken.None);
        await fixture.Scheduler.Received(1).EnqueueBootstrapAsync(account.Id, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_OtherCompanyAccount_ReturnsNotFoundAndDoesNotSchedule()
    {
        var fixture = CreateFixture();
        var account = CreateAccount(Guid.NewGuid());
        fixture.Repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Handler.Handle(
            new RequestUtilityAccountSyncCommand(account.Id), CancellationToken.None));

        Assert.Empty(fixture.Actions);
    }

    private static Fixture CreateFixture()
    {
        var companyId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        var repository = Substitute.For<IUtilityAccountRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var registrar = Substitute.For<IPostCommitRegistrar>();
        var scheduler = Substitute.For<IUtilityBillsJobScheduler>();
        var actions = new List<Func<CancellationToken, Task>>();
        tenantContext.CompanyId.Returns(companyId);
        currentUser.UserId.Returns(Guid.NewGuid());
        clock.UtcNow.Returns(now);
        registrar.When(x => x.RegisterPostCommitAction(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => actions.Add(call.Arg<Func<CancellationToken, Task>>()));
        return new Fixture(companyId, now, repository, scheduler, actions,
            new RequestUtilityAccountSyncCommandHandler(repository, tenantContext, currentUser, clock, registrar, scheduler));
    }

    private static UtilityAccount CreateAccount(Guid companyId) => UtilityAccount.Create(
        companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), UtilityType.Electricity,
        "1234567890", null, DateTimeOffset.UtcNow, Guid.NewGuid());

    private sealed record Fixture(
        Guid CompanyId,
        DateTimeOffset Now,
        IUtilityAccountRepository Repository,
        IUtilityBillsJobScheduler Scheduler,
        List<Func<CancellationToken, Task>> Actions,
        RequestUtilityAccountSyncCommandHandler Handler);
}
