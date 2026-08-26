using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public class LinkUtilityAccountCommandHandlerTests
{
    private readonly PropertyOsDbContext _dbContext;
    private readonly IUtilityAccountRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBusinessClock _clock;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly IUtilityBillsJobScheduler _jobScheduler;
    private readonly LinkUtilityAccountCommandHandler _handler;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);

    public LinkUtilityAccountCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PropertyOsDbContext(options);
        _repository = Substitute.For<IUtilityAccountRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _currentUserContext = Substitute.For<ICurrentUserContext>();
        _clock = Substitute.For<IBusinessClock>();
        _postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
        _jobScheduler = Substitute.For<IUtilityBillsJobScheduler>();

        _tenantContext.CompanyId.Returns(_companyId);
        _currentUserContext.UserId.Returns(_userId);
        _clock.UtcNow.Returns(_now);

        _handler = new LinkUtilityAccountCommandHandler(
            _dbContext,
            _repository,
            _tenantContext,
            _currentUserContext,
            _clock,
            _postCommitRegistrar,
            _jobScheduler);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAccountAndRegistersPostCommitJob()
    {
        Func<CancellationToken, Task>? postCommitAction = null;
        _postCommitRegistrar.RegisterPostCommitAction(
            Arg.Do<Func<CancellationToken, Task>>(action => postCommitAction = action));

        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var lease = LeaseContract.Create(
            companyId: _companyId,
            buildingId: buildingId,
            apartmentId: apartmentId,
            tenantId: tenantId,
            contractNumber: "LC-2026-001",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31),
            monthlyRentAmount: 500m,
            paymentFrequency: PaymentFrequency.Monthly,
            paymentDueDay: 1,
            createdAt: _now,
            createdBy: _userId);

        _dbContext.LeaseContracts.Add(lease);
        await _dbContext.SaveChangesAsync();

        _repository.ExistsByLeaseAndTypeAsync(lease.Id, UtilityType.Electricity, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new LinkUtilityAccountCommand(
            LeaseContractId: lease.Id,
            UtilityType: UtilityType.Electricity,
            AccountNumber: "ELEC-123456",
            MeterNumber: "MTR-999");

        var accountId = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, accountId);
        await _repository.Received(1).AddAsync(
            Arg.Is<UtilityAccount>(a =>
                a.CompanyId == _companyId &&
                a.LeaseContractId == lease.Id &&
                a.UtilityType == UtilityType.Electricity &&
                a.AccountNumber == "ELEC-123456" &&
                a.MeterNumber == "MTR-999" &&
                !a.HistoricalBootstrapCompleted),
            Arg.Any<CancellationToken>());

        _postCommitRegistrar.Received(1).RegisterPostCommitAction(Arg.Any<Func<CancellationToken, Task>>());
        Assert.NotNull(postCommitAction);

        await postCommitAction!(CancellationToken.None);
        await _jobScheduler.Received(1)
            .EnqueueBootstrapAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeaseNotFound_ThrowsNotFoundException()
    {
        var nonExistentLeaseId = Guid.NewGuid();
        var command = new LinkUtilityAccountCommand(
            LeaseContractId: nonExistentLeaseId,
            UtilityType: UtilityType.Electricity,
            AccountNumber: "ELEC-123456");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CrossTenantLease_ThrowsNotFoundException()
    {
        var otherCompanyId = Guid.NewGuid();
        var lease = LeaseContract.Create(
            companyId: otherCompanyId, // Different company
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            contractNumber: "LC-OTHER-001",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31),
            monthlyRentAmount: 500m,
            paymentFrequency: PaymentFrequency.Monthly,
            paymentDueDay: 1,
            createdAt: _now,
            createdBy: _userId);

        _dbContext.LeaseContracts.Add(lease);
        await _dbContext.SaveChangesAsync();

        var command = new LinkUtilityAccountCommand(
            LeaseContractId: lease.Id,
            UtilityType: UtilityType.Electricity,
            AccountNumber: "ELEC-123456");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateTypeOnSameLease_ThrowsConflictException()
    {
        var lease = LeaseContract.Create(
            companyId: _companyId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            contractNumber: "LC-2026-002",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31),
            monthlyRentAmount: 500m,
            paymentFrequency: PaymentFrequency.Monthly,
            paymentDueDay: 1,
            createdAt: _now,
            createdBy: _userId);

        _dbContext.LeaseContracts.Add(lease);
        await _dbContext.SaveChangesAsync();

        _repository.ExistsByLeaseAndTypeAsync(lease.Id, UtilityType.Water, Arg.Any<CancellationToken>())
            .Returns(true); // already linked

        var command = new LinkUtilityAccountCommand(
            LeaseContractId: lease.Id,
            UtilityType: UtilityType.Water,
            AccountNumber: "WTR-7777");

        await Assert.ThrowsAsync<UtilityAccountAlreadyLinkedException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExactSoftDeletedMatch_ReactivatesSameIdentityAndSchedulesIncrementalSync()
    {
        var lease = LeaseContract.Create(
            _companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "LC-RELINK", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            500m, PaymentFrequency.Monthly, 1, _now, _userId);
        _dbContext.LeaseContracts.Add(lease);
        await _dbContext.SaveChangesAsync();
        var existing = UtilityAccount.Create(
            _companyId, lease.Id, lease.TenantId, lease.ApartmentId,
            UtilityType.Electricity, "1234567890", null, _now, _userId);
        existing.MarkBootstrapCompleted(
            new DateOnly(2026, 4, 1), null, 0, null, _now, _now.AddDays(30));
        existing.SoftDelete(_now.AddHours(1), _userId);
        _repository.ExistsByLeaseAndTypeAsync(
            lease.Id, UtilityType.Electricity, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetDeletedExactMatchAsync(
            _companyId, lease.Id, UtilityType.Electricity, "1234567890",
            Arg.Any<CancellationToken>()).Returns(existing);
        Func<CancellationToken, Task>? action = null;
        _postCommitRegistrar.RegisterPostCommitAction(
            Arg.Do<Func<CancellationToken, Task>>(value => action = value));

        var result = await _handler.Handle(new LinkUtilityAccountCommand(
            lease.Id, UtilityType.Electricity, "1234567890", "M-2"), CancellationToken.None);

        Assert.Equal(existing.Id, result);
        Assert.True(existing.IsActive);
        Assert.Null(existing.DeletedAt);
        Assert.True(existing.HistoricalBootstrapCompleted);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        Assert.NotNull(action);
        await action!(CancellationToken.None);
        await _jobScheduler.Received(1).EnqueueSyncAsync(existing.Id, CancellationToken.None);
        await _jobScheduler.DidNotReceiveWithAnyArgs().EnqueueBootstrapAsync(default, default);
    }
}
