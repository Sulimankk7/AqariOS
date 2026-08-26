using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.TenantLinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.TenantReplaceUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.TenantUnlinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.ReplaceUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.UnlinkUtilityAccount;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public sealed class TenantLinkUtilityAccountCommandHandlerTests : IDisposable
{
    private readonly PropertyOsDbContext _db;
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    public TenantLinkUtilityAccountCommandHandlerTests()
    {
        _db = new PropertyOsDbContext(
            new DbContextOptionsBuilder<PropertyOsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _tenantContext.CompanyId.Returns(_companyId);
        _currentUserContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_ExactlyOneActiveLease_DelegatesToCanonicalLinkCommand()
    {
        var lease = await SeedTenantAndActiveLeaseAsync(_companyId, _userId);
        var expectedId = Guid.NewGuid();
        _sender.Send(Arg.Any<LinkUtilityAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new TenantLinkUtilityAccountCommand(
                UtilityType.Electricity, "0260004283", "METER-1"),
            CancellationToken.None);

        result.Should().Be(expectedId);
        await _sender.Received(1).Send(
            Arg.Is<LinkUtilityAccountCommand>(command =>
                command.LeaseContractId == lease.Id
                && command.UtilityType == UtilityType.Electricity
                && command.AccountNumber == "0260004283"
                && command.MeterNumber == "METER-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoActiveLease_FailsClosed()
    {
        await SeedTenantAsync(_companyId, _userId);
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(
            new TenantLinkUtilityAccountCommand(UtilityType.Water, "WATER-1"),
            CancellationToken.None));

        exception.Code.Should().Be("UTILITY_ACCOUNT_NO_ELIGIBLE_LEASE");
        await _sender.DidNotReceiveWithAnyArgs()
            .Send(default(LinkUtilityAccountCommand)!, default);
    }

    [Fact]
    public async Task Handle_MultipleActiveLeases_FailsClosedWithoutSelectingOne()
    {
        var tenant = await SeedTenantAsync(_companyId, _userId);
        await SeedActiveLeaseAsync(_companyId, tenant.Id, "A");
        await SeedActiveLeaseAsync(_companyId, tenant.Id, "B");
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(
            new TenantLinkUtilityAccountCommand(UtilityType.Electricity, "ELEC-1"),
            CancellationToken.None));

        exception.Code.Should().Be("UTILITY_ACCOUNT_ACTIVE_LEASE_AMBIGUOUS");
        await _sender.DidNotReceiveWithAnyArgs()
            .Send(default(LinkUtilityAccountCommand)!, default);
    }

    [Fact]
    public async Task Handle_LeaseInAnotherCompany_IsNotEligible()
    {
        var tenant = await SeedTenantAsync(_companyId, _userId);
        await SeedActiveLeaseAsync(Guid.NewGuid(), tenant.Id, "OTHER");
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(
            new TenantLinkUtilityAccountCommand(UtilityType.Electricity, "ELEC-1"),
            CancellationToken.None));

        exception.Code.Should().Be("UTILITY_ACCOUNT_NO_ELIGIBLE_LEASE");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void Validator_RejectsUnsupportedUtilityType(int value)
    {
        var validator = new TenantLinkUtilityAccountCommandValidator();

        var result = validator.Validate(new TenantLinkUtilityAccountCommand(
            (UtilityType)value, "ACCOUNT"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_UsesCanonicalAccountAndMeterLengthRules()
    {
        var validator = new TenantLinkUtilityAccountCommandValidator();

        validator.Validate(new TenantLinkUtilityAccountCommand(
                UtilityType.Electricity, "", new string('M', 101)))
            .Errors.Select(error => error.PropertyName)
            .Should().Contain(new[] { "AccountNumber", "MeterNumber" });
    }

    [Fact]
    public async Task TenantReplace_OwnCurrentLeaseAccount_DelegatesWithoutClientOwnershipIdentifiers()
    {
        var lease = await SeedTenantAndActiveLeaseAsync(_companyId, _userId);
        var tenantId = lease.TenantId;
        var account = UtilityAccount.Create(
            _companyId, lease.Id, tenantId, lease.ApartmentId,
            UtilityType.Electricity, "1234567890", null, _now, _userId);
        _db.UtilityAccounts.Add(account);
        await _db.SaveChangesAsync();
        var replacementId = Guid.NewGuid();
        _sender.Send(Arg.Any<ReplaceUtilityAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(replacementId);
        var handler = new TenantReplaceUtilityAccountCommandHandler(
            _db, _tenantContext, _currentUserContext, _sender);

        var result = await handler.Handle(new TenantReplaceUtilityAccountCommand(
            account.Id, "0987654321", "M-2"), CancellationToken.None);

        result.Should().Be(replacementId);
        await _sender.Received(1).Send(Arg.Is<ReplaceUtilityAccountCommand>(command =>
            command.UtilityAccountId == account.Id
            && command.AccountNumber == "0987654321"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantUnlink_AnotherTenantsAccount_FailsClosedAsNotFound()
    {
        await SeedTenantAndActiveLeaseAsync(_companyId, _userId);
        var otherTenant = await SeedTenantAsync(_companyId, Guid.NewGuid());
        var otherLease = await SeedActiveLeaseAsync(_companyId, otherTenant.Id, "OTHER-TENANT");
        var otherAccount = UtilityAccount.Create(
            _companyId, otherLease.Id, otherTenant.Id, otherLease.ApartmentId,
            UtilityType.Water, "12345", null, _now, _userId);
        _db.UtilityAccounts.Add(otherAccount);
        await _db.SaveChangesAsync();
        var handler = new TenantUnlinkUtilityAccountCommandHandler(
            _db, _tenantContext, _currentUserContext, _sender);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new TenantUnlinkUtilityAccountCommand(otherAccount.Id), CancellationToken.None));

        await _sender.DidNotReceiveWithAnyArgs().Send(default(UnlinkUtilityAccountCommand)!, default);
    }

    public void Dispose() => _db.Dispose();

    private TenantLinkUtilityAccountCommandHandler CreateHandler() =>
        new(_db, _tenantContext, _currentUserContext, _sender);

    private async Task<LeaseContract> SeedTenantAndActiveLeaseAsync(
        Guid companyId,
        Guid userId)
    {
        var tenant = await SeedTenantAsync(companyId, userId);
        return await SeedActiveLeaseAsync(companyId, tenant.Id, "ONLY");
    }

    private async Task<Tenant> SeedTenantAsync(Guid companyId, Guid userId)
    {
        var tenant = Tenant.Create(
            companyId, "Tenant", Guid.NewGuid().ToString("N"), "+962790000000",
            _now, userId, userId: userId);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return tenant;
    }

    private async Task<LeaseContract> SeedActiveLeaseAsync(
        Guid companyId,
        Guid tenantId,
        string suffix)
    {
        var buildingId = Guid.NewGuid();
        var building = Building.Create(
            companyId, $"Building {suffix}", 1, _now, _userId, id: buildingId);
        _db.Buildings.Add(building);

        var apartment = Apartment.Create(
            companyId, buildingId, Guid.NewGuid(), $"{suffix}-1", 80m, _now, _userId);
        _db.Apartments.Add(apartment);
        await _db.SaveChangesAsync();

        var lease = LeaseContract.Create(
            companyId, buildingId, apartment.Id, tenantId, $"LEASE-{suffix}",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 400m,
            PaymentFrequency.Monthly, 1, _now, _userId,
            status: ContractStatus.Active);
        _db.LeaseContracts.Add(lease);
        await _db.SaveChangesAsync();
        return lease;
    }
}
