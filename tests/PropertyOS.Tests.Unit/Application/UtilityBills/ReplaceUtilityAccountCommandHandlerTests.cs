using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.ReplaceUtilityAccount;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public sealed class ReplaceUtilityAccountCommandHandlerTests : IAsyncDisposable
{
    private readonly PropertyOsDbContext _db;
    private readonly IUtilityAccountRepository _repository = Substitute.For<IUtilityAccountRepository>();
    private readonly IPostCommitRegistrar _registrar = Substitute.For<IPostCommitRegistrar>();
    private readonly IUtilityBillsJobScheduler _scheduler = Substitute.For<IUtilityBillsJobScheduler>();
    private readonly List<Func<CancellationToken, Task>> _actions = [];
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    public ReplaceUtilityAccountCommandHandlerTests()
    {
        _db = new PropertyOsDbContext(new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _registrar.When(x => x.RegisterPostCommitAction(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => _actions.Add(call.Arg<Func<CancellationToken, Task>>()));
    }

    [Fact]
    public async Task DifferentNumber_SoftDeletesOld_CreatesNew_AndSchedulesAfterCommit()
    {
        var current = Create("1234567890");
        _db.UtilityAccounts.Add(current);
        await _db.SaveChangesAsync();
        _repository.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
        UtilityAccount? added = null;
        _repository.AddAsync(Arg.Do<UtilityAccount>(value => added = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ReplaceUtilityAccountCommand(current.Id, "0987654321", "M-2"), CancellationToken.None);

        Assert.NotNull(current.DeletedAt);
        Assert.False(current.IsActive);
        Assert.NotNull(added);
        Assert.Equal(result, added!.Id);
        Assert.Equal("0987654321", added.AccountNumber);
        await _scheduler.DidNotReceiveWithAnyArgs().EnqueueBootstrapAsync(default, default);
        Assert.Single(_actions);
        await _actions[0](CancellationToken.None);
        await _scheduler.Received(1).EnqueueBootstrapAsync(added.Id, CancellationToken.None);
    }

    [Fact]
    public async Task SameNumber_UpdatesMeterWithoutCreatingAnotherAccount()
    {
        var current = Create("1234567890");
        _repository.GetByIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);

        var result = await CreateHandler().Handle(
            new ReplaceUtilityAccountCommand(current.Id, "1234567890", "M-2"), CancellationToken.None);

        Assert.Equal(current.Id, result);
        Assert.Equal("1234567890", current.AccountNumber);
        Assert.Equal("M-2", current.MeterNumber);
        Assert.Null(current.DeletedAt);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    private ReplaceUtilityAccountCommandHandler CreateHandler()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CompanyId.Returns(_companyId);
        var user = Substitute.For<ICurrentUserContext>();
        user.UserId.Returns(_actorId);
        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(_now);
        var options = Substitute.For<IOptionsSnapshot<UtilityBillsOptions>>();
        options.Value.Returns(new UtilityBillsOptions { StaleClaimThresholdMinutes = 10 });
        return new ReplaceUtilityAccountCommandHandler(
            _db, _repository, tenant, user, clock, options, _registrar, _scheduler);
    }

    private UtilityAccount Create(string number) => UtilityAccount.Create(
        _companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        UtilityType.Electricity, number, null, _now.AddDays(-1), _actorId);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
