using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Subscriptions.UseCases;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Tests.Unit.Application.Subscriptions;

public sealed class GetCurrentPaygUsageQueryHandlerTests
{
    [Fact]
    public async Task CurrentUsage_IsLeaseBasedBackendCalculatedAndIdempotent()
    {
        var companyId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);
        await using var db = new PaygTestDbContext(new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(), Code = "PAYG", NameEn = "Pay as you go", NameAr = "الدفع حسب الاستخدام",
            PricingModel = SubscriptionPricingModel.PayAsYouGo, MonthlyPrice = 0, YearlyPrice = 0,
            PaygMonthlyUnitPrice = 3m, PaygYearlyMonthlyEquivalentUnitPrice = 2m,
            Currency = "JOD", FeatureFlags = "{}", CreatedAt = now, UpdatedAt = now
        };
        var subscription = new CompanySubscription
        {
            Id = Guid.NewGuid(), CompanyId = companyId, PlanId = plan.Id, Plan = plan,
            Status = SubscriptionStatusEnum.Active, BillingCycle = BillingCycleEnum.Monthly,
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2027, 1, 1),
            CurrencyAtSubscription = "JOD", CreatedAt = now, UpdatedAt = now
        };
        var buildingId = Guid.NewGuid();
        var building = Building.Create(companyId, "PAYG Building", 1, now, null, id: buildingId);
        var billedTenant = Tenant.Create(companyId, "Billed renter", "PAYG-1", "+962791111111", now, null);
        var tenantWithoutLease = Tenant.Create(companyId, "No lease renter", "PAYG-2", "+962792222222", now, null);

        db.AddRange(plan, subscription, building, billedTenant, tenantWithoutLease);
        await db.SaveChangesAsync();

        var apartments = Enumerable.Range(1, 4)
            .Select(number => Apartment.Create(companyId, buildingId, Guid.NewGuid(), $"P-{number}", 80, now, null))
            .ToArray();
        db.Apartments.AddRange(apartments);
        await db.SaveChangesAsync();

        var fullLeaseActivatedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var laterLeaseActivatedAt = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var fullLease = LeaseContract.Create(companyId, buildingId, apartments[0].Id, billedTenant.Id,
            "PAYG-LEASE-1", new DateOnly(2026, 8, 1), new DateOnly(2027, 1, 1), 500,
            PaymentFrequency.Monthly, 1, fullLeaseActivatedAt, null);
        var laterLeaseForSameTenant = LeaseContract.Create(companyId, buildingId, apartments[1].Id, billedTenant.Id,
            "PAYG-LEASE-2", new DateOnly(2026, 8, 10), new DateOnly(2027, 1, 1), 500,
            PaymentFrequency.Monthly, 1, laterLeaseActivatedAt, null);
        var deletedLease = LeaseContract.Create(companyId, buildingId, apartments[2].Id, billedTenant.Id,
            "PAYG-LEASE-3", new DateOnly(2026, 8, 1), new DateOnly(2027, 1, 1), 500,
            PaymentFrequency.Monthly, 1, fullLeaseActivatedAt, null);
        var draftLease = LeaseContract.Create(companyId, buildingId, apartments[3].Id, billedTenant.Id,
            "PAYG-LEASE-DRAFT", new DateOnly(2026, 8, 1), new DateOnly(2027, 1, 1), 500,
            PaymentFrequency.Monthly, 1, now, null);
        db.LeaseContracts.AddRange(fullLease, laterLeaseForSameTenant, deletedLease, draftLease);
        await db.SaveChangesAsync();

        fullLease.Activate(fullLeaseActivatedAt, null);
        laterLeaseForSameTenant.Activate(laterLeaseActivatedAt, null);
        deletedLease.Activate(fullLeaseActivatedAt, null);
        db.ContractStatusHistory.AddRange(
            ContractStatusHistory.Create(companyId, fullLease.Id, ContractStatus.Active,
                fullLeaseActivatedAt, ContractStatus.Draft, reason: "Activated contract"),
            ContractStatusHistory.Create(companyId, laterLeaseForSameTenant.Id, ContractStatus.Active,
                laterLeaseActivatedAt, ContractStatus.Draft, reason: "Activated contract"),
            ContractStatusHistory.Create(companyId, deletedLease.Id, ContractStatus.Active,
                fullLeaseActivatedAt, ContractStatus.Draft, reason: "Activated contract"));
        db.ContractTerminations.Add(ContractTermination.Create(
            companyId, deletedLease.Id, TerminationType.EarlyTermination, new DateOnly(2026, 8, 8),
            0, 0, 0, null, true, "JOD", "PAYG test termination", null, null, fullLeaseActivatedAt, null));
        await db.SaveChangesAsync();

        deletedLease.SoftDelete(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero), null);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        (await db.SubscriptionPlans.AsNoTracking().SingleAsync()).PricingModel
            .Should().Be(SubscriptionPricingModel.PayAsYouGo);
        (await db.CompanySubscriptions.AsNoTracking().SingleAsync()).Status
            .Should().Be(SubscriptionStatusEnum.Active);

        var projectedLeases = await db.LeaseContracts.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId
                && x.StartDate < new DateOnly(2026, 8, 17)
                && x.EndDate > new DateOnly(2026, 8, 1))
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.DeletedAt,
                TenantDeletedAt = db.Tenants.IgnoreQueryFilters()
                    .Where(t => t.Id == x.TenantId && t.CompanyId == companyId)
                    .Select(t => t.DeletedAt).FirstOrDefault(),
                BuildingDeletedAt = db.Buildings.IgnoreQueryFilters()
                    .Where(b => b.Id == x.BuildingId && b.CompanyId == companyId)
                    .Select(b => b.DeletedAt).FirstOrDefault(),
                ApartmentDeletedAt = db.Apartments.IgnoreQueryFilters()
                    .Where(a => a.Id == x.ApartmentId && a.CompanyId == companyId)
                    .Select(a => a.DeletedAt).FirstOrDefault()
            })
            .ToListAsync();

        projectedLeases.Count(x => x.Status == ContractStatus.Active
            && x.DeletedAt is null
            && x.TenantDeletedAt is null
            && x.BuildingDeletedAt is null
            && x.ApartmentDeletedAt is null).Should().Be(2);

        var handler = new GetCurrentPaygUsageQueryHandler(
            db, new TestTenantContext(companyId), new TestBusinessClock(now));
        var first = await handler.Handle(new GetCurrentPaygUsageQuery(now), CancellationToken.None);
        await db.SaveChangesAsync();
        var second = await handler.Handle(new GetCurrentPaygUsageQuery(now), CancellationToken.None);
        await db.SaveChangesAsync();

        first.CurrentActiveLeaseCount.Should().Be(2);
        first.AccumulatedLeaseDays.Should().Be(30);
        first.EstimatedAmount.Should().Be(2.902m);
        first.ProjectedPeriodAmount.Should().Be(5.805m);
        second.Should().BeEquivalentTo(first);
        db.PaygUsagePeriods.Should().ContainSingle();
        db.PaygLeaseUsage.Should().HaveCount(3);
        db.PaygLeaseUsage.Select(x => x.LeaseContractId).Should().OnlyHaveUniqueItems();
        db.PaygLeaseUsage.Single(x => x.LeaseContractId == fullLease.Id).BillableDays.Should().Be(16);
        db.PaygLeaseUsage.Single(x => x.LeaseContractId == laterLeaseForSameTenant.Id).BillableDays.Should().Be(7);
        db.PaygLeaseUsage.Single(x => x.LeaseContractId == deletedLease.Id).BillableDays.Should().Be(7);
        db.PaygLeaseUsage.Single(x => x.LeaseContractId == deletedLease.Id).UsageEnd.Should().Be(new DateOnly(2026, 8, 8));
        db.PaygLeaseUsage.Select(x => x.LeaseContractId).Should().NotContain(draftLease.Id);
        db.PaygLeaseUsage.Select(x => x.TenantId).Distinct().Should().Equal(billedTenant.Id);
        db.PaygLeaseUsage.Select(x => x.TenantId).Should().NotContain(tenantWithoutLease.Id);
    }

    private sealed class TestTenantContext(Guid companyId) : ITenantContext
    {
        public Guid? CompanyId { get; } = companyId;
        public bool IsPlatformAdmin => false;
    }

    private sealed class TestBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset UtcNow { get; } = now;
        public DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null) =>
            DateOnly.FromDateTime((utcInstant ?? now).UtcDateTime);
    }

    private sealed class PaygTestDbContext(DbContextOptions<PropertyOsDbContext> options)
        : PropertyOsDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SubscriptionPlan>()
                .Property(x => x.PricingModel)
                .ValueGeneratedNever();
            modelBuilder.Entity<CompanySubscription>()
                .Property(x => x.Status)
                .ValueGeneratedNever();
            modelBuilder.Entity<LeaseContract>()
                .Property(x => x.Status)
                .ValueGeneratedNever();
        }
    }

}
