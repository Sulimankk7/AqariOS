using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;
using System;
using System.Threading.Tasks;

namespace PropertyOS.Tests.Integration.Subscriptions;

[Collection("Postgres collection")]
public class Module2SubscriptionsIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public Module2SubscriptionsIntegrationTests(PostgresTestFixture fixture)
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

    private PropertyOsDbContext CreateContext(Guid? companyId, bool isPlatformAdmin = false)
    {
        var tenantContext = new StaticTenantContext { CompanyId = companyId, IsPlatformAdmin = isPlatformAdmin };
        var userContext = new StaticCurrentUserContext { UserId = null };

        var connection = isPlatformAdmin 
            ? _fixture.Context.Database.GetDbConnection() 
            : _sharedAppUserConnection!;

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(connection, o => 
            {
                o.MapEnum<CompanyType>("company_type_enum");
                o.MapEnum<LateFeeType>("late_fee_type_enum");
                o.MapEnum<SubscriptionStatusEnum>("subscription_status_enum");
                o.MapEnum<BillingCycleEnum>("billing_cycle_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task SubscriptionPlan_GlobalVisibility_IgnoresTenantContext()
    {
        // 1. Setup as Platform Admin
        await using var adminCtx = CreateContext(null, isPlatformAdmin: true);
        await adminCtx.Database.BeginTransactionAsync();
        
        var plan = new SubscriptionPlan
        {
            Code = "test_plan",
            NameEn = "Test Plan",
            NameAr = "خطة اختبار",
            MonthlyPrice = 100,
            YearlyPrice = 1000,
            Currency = "JOD",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        adminCtx.SubscriptionPlans.Add(plan);
        await adminCtx.SaveChangesAsync();
        await adminCtx.Database.CommitTransactionAsync();

        // 2. Query as an unauthenticated/tenant-missing user (missing context)
        await using var tenantCtx = CreateContext(null);
        await tenantCtx.Database.BeginTransactionAsync();
        
        var plans = await tenantCtx.SubscriptionPlans.ToListAsync();
        
        // RLS should not apply to SubscriptionPlan, so the plan must be visible
        plans.Should().ContainSingle(p => p.Code == "test_plan");
        
        await tenantCtx.Database.RollbackTransactionAsync();
    }

    [Fact]
    public async Task PostgresConstraints_CompanySubscriptions_EnforcesInvariants()
    {
        await using var adminCtx = CreateContext(null, isPlatformAdmin: true);
        await adminCtx.Database.BeginTransactionAsync();
        
        var planId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        
        // Setup missing FK records
        await adminCtx.Database.ExecuteSqlRawAsync(
            "INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, country_code) VALUES ({0}, 'A', 'A', '+962790000001', 'individual_owner', 'JO')", companyId);
        await adminCtx.Database.ExecuteSqlRawAsync(
            "INSERT INTO subscription_plans (id, code, name_en, name_ar, monthly_price, yearly_price, currency) VALUES ({0}, 'test', 'Test', 'Test', 10, 100, 'JOD')", planId);

        // 1. Test Date constraint: end_date < start_date
        await adminCtx.Database.ExecuteSqlRawAsync("SAVEPOINT before_date_test");
        var dateEx = await Record.ExceptionAsync(async () => await adminCtx.Database.ExecuteSqlRawAsync(
            "INSERT INTO company_subscriptions (company_id, plan_id, status, start_date, end_date, price_at_subscription) VALUES ({0}, {1}, 'active', '2026-02-01', '2026-01-01', 10)", companyId, planId));
        dateEx.Should().BeOfType<PostgresException>().Which.Message.Should().Contain("chk_company_subscriptions_dates");
        await adminCtx.Database.ExecuteSqlRawAsync("ROLLBACK TO SAVEPOINT before_date_test");

        // 2. Test Trial constraint: trial_end_date IS NULL WHEN status = 'trialing'
        await adminCtx.Database.ExecuteSqlRawAsync("SAVEPOINT before_trial_test");
        var trialEx = await Record.ExceptionAsync(async () => await adminCtx.Database.ExecuteSqlRawAsync(
            "INSERT INTO company_subscriptions (company_id, plan_id, status, start_date, end_date, price_at_subscription) VALUES ({0}, {1}, 'trialing', '2026-01-01', '2026-02-01', 10)", companyId, planId));
        trialEx.Should().BeOfType<PostgresException>().Which.Message.Should().Contain("chk_company_subscriptions_trial_end");
        await adminCtx.Database.ExecuteSqlRawAsync("ROLLBACK TO SAVEPOINT before_trial_test");

        await adminCtx.Database.RollbackTransactionAsync();
    }

    [Fact]
    public async Task Concurrency_OneActiveSubscription_PartialUniqueIndex_Rejects_RaceCondition()
    {
        // Setup Company and Plan using admin context
        var companyId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            await adminCtx.Database.BeginTransactionAsync();
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, country_code) VALUES ({0}, 'A', 'A', '+962790000001', 'individual_owner', 'JO')", companyId);
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO subscription_plans (id, code, name_en, name_ar, monthly_price, yearly_price, currency) VALUES ({0}, 'p1', 'Test', 'Test', 10, 100, 'JOD')", planId);
            await adminCtx.Database.CommitTransactionAsync();
        }

        // We will create two completely independent database connections directly to test PostgreSQL concurrency.
        // EF Core SaveChanges can sometimes behave differently due to connection pooling, but using NpgsqlConnection
        // explicitly with two distinct data sources or connections ensures they run concurrently.
        await using var conn1 = await _fixture.AppUserDataSource!.OpenConnectionAsync();
        await using var conn2 = await _fixture.AppUserDataSource!.OpenConnectionAsync();

        var ready1 = new TaskCompletionSource();
        var ready2 = new TaskCompletionSource();
        var goSource = new TaskCompletionSource();

        async Task RunConcurrentInsert(NpgsqlConnection conn, TaskCompletionSource ready)
        {
            // 1. Begin explicit transaction
            await using var tx = await conn.BeginTransactionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx; // 2. Associate commands with transaction
            
            // 3. Set tenant context in this transaction
            cmd.CommandText = "SELECT set_config('app.current_company_id', @companyId::text, true)";
            cmd.Parameters.AddWithValue("companyId", companyId);
            await cmd.ExecuteNonQueryAsync();

            // Prepare the conflicting insert
            cmd.CommandText = @"
                INSERT INTO company_subscriptions (company_id, plan_id, status, start_date, end_date, trial_end_date, price_at_subscription) 
                VALUES (@companyId, @planId, 'active', '2026-01-01', '2026-12-31', null, 10)";
            cmd.Parameters.AddWithValue("planId", planId);

            // Signal that transaction is open and context is set
            ready.SetResult();
            
            // Wait for both connections to be in their transactions simultaneously
            await goSource.Task;

            // 4. Execute the insert concurrently inside the same transaction
            await cmd.ExecuteNonQueryAsync();
            
            // 5. Commit explicitly
            await tx.CommitAsync();
        }

        var t1 = RunConcurrentInsert(conn1, ready1);
        var t2 = RunConcurrentInsert(conn2, ready2);

        // Wait for BOTH independent transactions to be explicitly opened and ready
        await Task.WhenAll(ready1.Task, ready2.Task);
        
        // Go! Both operations now genuinely overlap and race on the INSERT/COMMIT
        goSource.SetResult();

        // One should succeed, one should fail with PostgresException 23505 (unique_violation)
        Exception? exception = null;
        try
        {
            await Task.WhenAll(t1, t2);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        exception.Should().NotBeNull();
        exception.Should().BeOfType<PostgresException>();
        ((PostgresException)exception!).SqlState.Should().Be("23505");
        ((PostgresException)exception!).ConstraintName.Should().Be("uq_company_subscriptions_one_active");

        // Verify only 1 active subscription exists
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            var activeCount = await adminCtx.CompanySubscriptions.CountAsync(s => s.CompanyId == companyId && s.Status == SubscriptionStatusEnum.Active);
            activeCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CompanySubscription_TenantIsolation_EnforcedByRLS()
    {
        var companyA_Id = Guid.NewGuid();
        var companyB_Id = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            await adminCtx.Database.BeginTransactionAsync();
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, country_code) VALUES ({0}, 'A', 'A', '+962790000001', 'individual_owner', 'JO')", companyA_Id);
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, country_code) VALUES ({0}, 'B', 'B', '+962790000002', 'individual_owner', 'JO')", companyB_Id);
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO subscription_plans (id, code, name_en, name_ar, monthly_price, yearly_price, currency) VALUES ({0}, 'p1', 'Test', 'Test', 10, 100, 'JOD')", planId);
            
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO company_subscriptions (company_id, plan_id, status, start_date, end_date, trial_end_date, price_at_subscription) VALUES ({0}, {1}, 'active', '2026-01-01', '2026-12-31', null, 10)", companyA_Id, planId);
            await adminCtx.Database.ExecuteSqlRawAsync(
                "INSERT INTO company_subscriptions (company_id, plan_id, status, start_date, end_date, trial_end_date, price_at_subscription) VALUES ({0}, {1}, 'active', '2026-01-01', '2026-12-31', null, 10)", companyB_Id, planId);

            await adminCtx.Database.CommitTransactionAsync();
        }

        // Query as Company A
        await using (var ctxA = CreateContext(companyA_Id))
        {
            await ctxA.Database.BeginTransactionAsync();

            var subs = await ctxA.CompanySubscriptions.IgnoreQueryFilters().ToListAsync();
            subs.Should().HaveCount(1);
            subs[0].CompanyId.Should().Be(companyA_Id);

            // Cross-tenant INSERT fails
            await ctxA.Database.ExecuteSqlRawAsync("SAVEPOINT before_insert");
            var act = async () => await ctxA.Database.ExecuteSqlRawAsync(
                "INSERT INTO company_subscriptions (company_id, plan_id, status, start_date, end_date, price_at_subscription) VALUES ({0}, {1}, 'active', '2027-01-01', '2027-12-31', 10)", companyB_Id, planId);
            var ex = await act.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("42501");
            await ctxA.Database.ExecuteSqlRawAsync("ROLLBACK TO SAVEPOINT before_insert");

            // Cross-tenant UPDATE fails
            var rowsAffected = await ctxA.Database.ExecuteSqlRawAsync(
                "UPDATE company_subscriptions SET price_at_subscription = 50 WHERE company_id = {0}", companyB_Id);
            rowsAffected.Should().Be(0);

            await ctxA.Database.RollbackTransactionAsync();
        }

        // Missing tenant context fails closed
        await using (var ctxNoTenant = CreateContext(null))
        {
            await ctxNoTenant.Database.BeginTransactionAsync();
            var count = await ctxNoTenant.CompanySubscriptions.CountAsync();
            count.Should().Be(0);
            await ctxNoTenant.Database.RollbackTransactionAsync();
        }
    }
}
