using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Rls;

[Collection("Postgres collection")]
public class FinancialsRlsTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public FinancialsRlsTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _sharedAppUserConnection = await _fixture.AppUserDataSource!.OpenConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_sharedAppUserConnection != null)
            await _sharedAppUserConnection.DisposeAsync();
    }

    private sealed class StaticTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin { get; set; }
    }

    private sealed class StaticCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; }
    }

    private PropertyOsDbContext CreateTenantContext(Guid? companyId)
    {
        var tenantContext = new StaticTenantContext { CompanyId = companyId, IsPlatformAdmin = false };
        var userContext = new StaticCurrentUserContext { UserId = Guid.NewGuid() };

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_sharedAppUserConnection!, o =>
            {
                o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
                o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
                o.MapEnum<ExpenseCategory>("expense_category_enum");
                o.MapEnum<ExpensePaymentMethod>("expense_payment_method_enum");
                o.MapEnum<ReceiptResetPolicy>("receipt_reset_policy_enum");
                o.MapEnum<EfawateercomStatus>("efawateercom_status_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    private PropertyOsDbContext CreateAdminContext()
    {
        var tenantContext = new StaticTenantContext { CompanyId = null, IsPlatformAdmin = true };
        var userContext = new StaticCurrentUserContext { UserId = Guid.NewGuid() };

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_fixture.Context.Database.GetDbConnection(), o =>
            {
                o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
                o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
                o.MapEnum<ExpenseCategory>("expense_category_enum");
                o.MapEnum<ExpensePaymentMethod>("expense_payment_method_enum");
                o.MapEnum<ReceiptResetPolicy>("receipt_reset_policy_enum");
                o.MapEnum<EfawateercomStatus>("efawateercom_status_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Rls_TenantIsolation_EnforcedForExpensesAndSequences()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        // 1. Seed companies as Platform Admin (using superuser context)
        await using (var adminCtx = CreateAdminContext())
        {
            await adminCtx.Database.BeginTransactionAsync();

            var compA = PropertyOS.Domain.Companies.Company.Create(
                "Company A", "Company A", "+962791111111", PropertyOS.Domain.Companies.Enums.CompanyType.IndividualOwner,
                "JO", DateTimeOffset.UtcNow, null, "CR-A", "TAX-A", "a@company.com");
            var compB = PropertyOS.Domain.Companies.Company.Create(
                "Company B", "Company B", "+962792222222", PropertyOS.Domain.Companies.Enums.CompanyType.IndividualOwner,
                "JO", DateTimeOffset.UtcNow, null, "CR-B", "TAX-B", "b@company.com");

            adminCtx.Set<PropertyOS.Domain.Companies.Company>().Add(compA);
            adminCtx.Set<PropertyOS.Domain.Companies.Company>().Add(compB);
            await adminCtx.SaveChangesAsync();

            // Set real IDs assigned by PG
            companyA = compA.Id;
            companyB = compB.Id;

            // Seed settings and expense records
            var expA = Expense.Create(companyA, null, ExpenseCategory.Elevator, 150m, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), ExpensePaymentMethod.Cash, "Clean elevator A");
            var expB = Expense.Create(companyB, null, ExpenseCategory.Cleaning, 80m, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), ExpensePaymentMethod.Cash, "Clean office B");

            adminCtx.Expenses.Add(expA);
            adminCtx.Expenses.Add(expB);

            // Seed sequences
            var seqA = CompanyReceiptSequence.Create(companyA, "INV-A-", 5, ReceiptResetPolicy.Never, DateTimeOffset.UtcNow);
            var seqB = CompanyReceiptSequence.Create(companyB, "INV-B-", 5, ReceiptResetPolicy.Never, DateTimeOffset.UtcNow);
            adminCtx.CompanyReceiptSequences.Add(seqA);
            adminCtx.CompanyReceiptSequences.Add(seqB);

            await adminCtx.SaveChangesAsync();
            await adminCtx.Database.CommitTransactionAsync();
        }

        // 2. Query as Tenant A — should ONLY see Tenant A's records
        await using (var tenantA_Ctx = CreateTenantContext(companyA))
        {
            await tenantA_Ctx.Database.BeginTransactionAsync();

            var expenses = await tenantA_Ctx.Expenses.ToListAsync();
            Assert.Single(expenses);
            Assert.Equal(companyA, expenses[0].CompanyId);

            var seq = await tenantA_Ctx.CompanyReceiptSequences.FirstOrDefaultAsync();
            Assert.NotNull(seq);
            Assert.Equal(companyA, seq.CompanyId);
            Assert.Equal("INV-A-", seq.Prefix);

            await tenantA_Ctx.Database.RollbackTransactionAsync();
        }

        // 3. Query as Tenant B — should ONLY see Tenant B's records
        await using (var tenantB_Ctx = CreateTenantContext(companyB))
        {
            await tenantB_Ctx.Database.BeginTransactionAsync();

            var expenses = await tenantB_Ctx.Expenses.ToListAsync();
            Assert.Single(expenses);
            Assert.Equal(companyB, expenses[0].CompanyId);

            var seq = await tenantB_Ctx.CompanyReceiptSequences.FirstOrDefaultAsync();
            Assert.NotNull(seq);
            Assert.Equal(companyB, seq.CompanyId);
            Assert.Equal("INV-B-", seq.Prefix);

            await tenantB_Ctx.Database.RollbackTransactionAsync();
        }
    }
}
