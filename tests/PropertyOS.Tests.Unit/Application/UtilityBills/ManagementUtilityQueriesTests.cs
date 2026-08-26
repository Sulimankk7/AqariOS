using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityAccounts;
using PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityBills;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public sealed class ManagementUtilityQueriesTests
{
    [Fact]
    public async Task AccountList_ProjectsContextAndFiltersAtCompanyBoundary()
    {
        await using var db = CreateDbContext();
        var companyId = Guid.NewGuid();
        var expected = SeedGraph(db, companyId, "Alpha", "101");
        SeedGraph(db, Guid.NewGuid(), "Other", "999");
        await db.SaveChangesAsync();

        var handler = new GetManagementUtilityAccountsQueryHandler(
            db, TenantContext(companyId), Options());

        var page = await handler.Handle(
            new GetManagementUtilityAccountsQuery(PageSize: 10),
            CancellationToken.None);

        page.Items.Should().ContainSingle();
        var account = page.Items.Single();
        account.Id.Should().Be(expected.Account.Id);
        account.BuildingName.Should().Be("Alpha Building");
        account.UnitNumber.Should().Be("101");
        account.LeaseContractNumber.Should().Be(expected.Lease.ContractNumber);
        account.TenantName.Should().Be("Alpha Tenant");
        account.ProviderAvailability.Should().Be(UtilityProviderAvailability.Disabled);
    }

    [Fact]
    public async Task AccountList_UsesBoundedDeterministicKeysetPagination()
    {
        await using var db = CreateDbContext();
        var companyId = Guid.NewGuid();
        SeedGraph(db, companyId, "Older", "101", DateTimeOffset.UtcNow.AddMinutes(-1));
        SeedGraph(db, companyId, "Newer", "102", DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        var handler = new GetManagementUtilityAccountsQueryHandler(
            db, TenantContext(companyId), Options());

        var first = await handler.Handle(
            new GetManagementUtilityAccountsQuery(PageSize: 1),
            CancellationToken.None);
        var second = await handler.Handle(
            new GetManagementUtilityAccountsQuery(Cursor: first.NextCursor, PageSize: 1),
            CancellationToken.None);

        first.Items.Should().ContainSingle();
        first.HasMore.Should().BeTrue();
        first.NextCursor.Should().NotBeNullOrWhiteSpace();
        second.Items.Should().ContainSingle();
        second.Items[0].Id.Should().NotBe(first.Items[0].Id);
        second.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task BillHistory_CrossCompanyAccountId_ReturnsSecureNotFound()
    {
        await using var db = CreateDbContext();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var graph = SeedGraph(db, companyA, "Alpha", "101");
        await db.SaveChangesAsync();

        var handler = new GetManagementUtilityBillsQueryHandler(
            db, TenantContext(companyB));

        await FluentActions.Invoking(() => handler.Handle(
                new GetManagementUtilityBillsQuery(graph.Account.Id),
                CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task BillHistory_ReturnsOnlyRequestedCompanyScopedAccountBills()
    {
        await using var db = CreateDbContext();
        var companyId = Guid.NewGuid();
        var graph = SeedGraph(db, companyId, "Alpha", "101");
        db.UtilityBills.Add(UtilityBill.Create(
            companyId,
            graph.Account.Id,
            UtilityType.Electricity,
            "BILL-1",
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 20),
            25m,
            "JOD",
            false,
            UtilityBillPaymentStatus.Unpaid,
            null,
            false,
            DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var handler = new GetManagementUtilityBillsQueryHandler(
            db, TenantContext(companyId));

        var page = await handler.Handle(
            new GetManagementUtilityBillsQuery(
                graph.Account.Id,
                PageSize: 10,
                PaymentStatus: UtilityBillPaymentStatus.Unpaid),
            CancellationToken.None);

        page.Items.Should().ContainSingle();
        page.Items[0].Amount.Should().Be(25m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void AccountListValidator_RejectsUnboundedPageSizes(int pageSize)
    {
        var result = new GetManagementUtilityAccountsQueryValidator().Validate(
            new GetManagementUtilityAccountsQuery(PageSize: pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void BillHistoryValidator_RejectsUnboundedPageSizes(int pageSize)
    {
        var result = new GetManagementUtilityBillsQueryValidator().Validate(
            new GetManagementUtilityBillsQuery(Guid.NewGuid(), PageSize: pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ManagementDto_DoesNotExposeRawProviderErrorDetail()
    {
        typeof(ManagementUtilityAccountDto).GetProperties()
            .Select(property => property.Name)
            .Should().NotContain(name => name.Contains("ErrorDetail", StringComparison.OrdinalIgnoreCase));
    }

    private static ITenantContext TenantContext(Guid companyId)
    {
        var context = Substitute.For<ITenantContext>();
        context.CompanyId.Returns(companyId);
        return context;
    }

    private static IOptionsSnapshot<UtilityBillsOptions> Options()
    {
        var snapshot = Substitute.For<IOptionsSnapshot<UtilityBillsOptions>>();
        snapshot.Value.Returns(new UtilityBillsOptions());
        return snapshot;
    }

    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private static Graph SeedGraph(
        PropertyOsDbContext db,
        Guid companyId,
        string prefix,
        string unitNumber,
        DateTimeOffset? createdAt = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        var buildingId = Guid.NewGuid();
        var building = Building.Create(
            companyId, $"{prefix} Building", 1, now, null, id: buildingId);
        var apartment = Apartment.Create(
            companyId, buildingId, Guid.NewGuid(), unitNumber, 80m, now, null);
        var tenant = Tenant.Create(
            companyId,
            $"{prefix} Tenant",
            Guid.NewGuid().ToString("N")[..10],
            $"+96279{Random.Shared.Next(1000000, 9999999)}",
            now,
            null);

        // Apartment IDs are generated by EF when the entity starts tracking,
        // mirroring the production database-generated key contract.
        db.Buildings.Add(building);
        db.Apartments.Add(apartment);
        db.Tenants.Add(tenant);

        var lease = LeaseContract.Create(
            companyId,
            building.Id,
            apartment.Id,
            tenant.Id,
            $"LEASE-{Guid.NewGuid():N}",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            400m,
            PaymentFrequency.Monthly,
            1,
            now,
            null);
        var account = UtilityAccount.Create(
            companyId,
            lease.Id,
            tenant.Id,
            apartment.Id,
            UtilityType.Electricity,
            $"ELEC-{Guid.NewGuid():N}",
            null,
            now,
            null);

        db.LeaseContracts.Add(lease);
        db.UtilityAccounts.Add(account);
        return new Graph(account, lease);
    }

    private sealed record Graph(UtilityAccount Account, LeaseContract Lease);
}
