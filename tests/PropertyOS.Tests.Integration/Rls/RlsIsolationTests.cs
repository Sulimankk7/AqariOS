using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Rls;

[Collection("Postgres collection")]
public class RlsIsolationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public RlsIsolationTests(PostgresTestFixture fixture)
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
        var userContext = new StaticCurrentUserContext { UserId = null }; // Module 1 test

        // In Phase 2, the RLS policy does not yet check app.is_platform_admin (deferred to Phase 8).
        // Therefore, a NOSUPERUSER cannot bypass RLS. For platform admin (seeding) operations,
        // we must use the superuser connection. For tenant operations, we use the restricted app_user connection.
        var connection = isPlatformAdmin 
            ? _fixture.Context.Database.GetDbConnection() 
            : _sharedAppUserConnection!;

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(connection, o => 
            {
                o.MapEnum<CompanyType>("company_type_enum");
                o.MapEnum<LateFeeType>("late_fee_type_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task TenantIsolation_EnforcedAtDatabaseLevel_EvenWhenQueryFiltersAreBypassed()
    {
        // Arrange
        var companyA_Id = Guid.Empty;
        var companyB_Id = Guid.Empty;

        // 1. Setup seed data as Platform Admin (bypasses RLS logic via SET LOCAL app.is_platform_admin = true)
        // Note: For EF Core, we actually just use the DbContext directly without transaction interceptor to seed.
        // Wait, if RLS is enforced, and propertyos role is NOSUPERUSER, even seeding requires context!
        // We will seed using the platform admin context.
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            await adminCtx.Database.BeginTransactionAsync();
            var compA = Company.Create(
                "Legal A", "Display A", "+962790000001", CompanyType.PropertyManagementCompany,
                "JO", DateTimeOffset.UtcNow, null, "CR1", "TAX1", "a@test.com");
            var compB = Company.Create(
                "Legal B", "Display B", "+962790000002", CompanyType.PropertyManagementCompany,
                "JO", DateTimeOffset.UtcNow, null, "CR2", "TAX2", "b@test.com");

            adminCtx.Set<Company>().Add(compA);
            adminCtx.Set<Company>().Add(compB);
            await adminCtx.SaveChangesAsync();

            companyA_Id = compA.Id;
            companyB_Id = compB.Id;

            var settingsA = CompanySettings.CreateWithDefaults(companyA_Id, DateTimeOffset.UtcNow);
            var settingsB = CompanySettings.CreateWithDefaults(companyB_Id, DateTimeOffset.UtcNow);
            settingsB.UpdateLateFeePolicy(LateFeeType.Fixed, 50m, DateTimeOffset.UtcNow);
            settingsB.UpdateFiscalYearStartMonth(1, DateTimeOffset.UtcNow);

            adminCtx.Set<CompanySettings>().Add(settingsA);
            adminCtx.Set<CompanySettings>().Add(settingsB);
            
            await adminCtx.SaveChangesAsync();
            await adminCtx.Database.CommitTransactionAsync();
        }

        // 2. Query as Company A
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            var compA = await adminCtx.Set<Company>().FirstOrDefaultAsync(c => c.LegalName == "Legal A");
            if (compA != null)
            {
                await using (var ctxA = CreateContext(companyA_Id))
                {
                    await ctxA.Database.BeginTransactionAsync(); // Forces TenantSessionInterceptor

                    // Company A should see exactly 1 company (itself) even with IgnoreQueryFilters()
                    var companiesA = await ctxA.Set<Company>().IgnoreQueryFilters().ToListAsync();
                    companiesA.Should().HaveCount(1);
                    companiesA[0].Id.Should().Be(companyA_Id);

                    var settingsA = await ctxA.Set<CompanySettings>().IgnoreQueryFilters().ToListAsync();
                    settingsA.Should().HaveCount(1);
                    settingsA[0].CompanyId.Should().Be(companyA_Id);

                    await ctxA.Database.RollbackTransactionAsync();
                }
            }
        }

        // 3. Query as Company B
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            var compB = await adminCtx.Set<Company>().FirstOrDefaultAsync(c => c.LegalName == "Legal B");
            if (compB != null)
            {
                await using (var ctxB = CreateContext(companyB_Id))
                {
                    await ctxB.Database.BeginTransactionAsync();

                    var companiesB = await ctxB.Set<Company>().IgnoreQueryFilters().ToListAsync();
                    companiesB.Should().HaveCount(1);
                    companiesB[0].Id.Should().Be(companyB_Id);

                    var settingsB = await ctxB.Set<CompanySettings>().IgnoreQueryFilters().ToListAsync();
                    settingsB.Should().HaveCount(1);
                    settingsB[0].CompanyId.Should().Be(companyB_Id);

                    await ctxB.Database.RollbackTransactionAsync();
                }
            }
        }

        // 4. Test Cross-Tenant Update Rejection (Company A tries to update Company B)
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            // We need to know Company B's ID, which we got earlier
            var compB = await adminCtx.Set<Company>().FirstOrDefaultAsync(c => c.LegalName == "Legal B");
            if (compB != null)
            {
                await using (var ctxA_Update = CreateContext(companyA_Id))
                {
                    await ctxA_Update.Database.BeginTransactionAsync();
                    
                    int rowsAffected = await ctxA_Update.Database.ExecuteSqlRawAsync(
                        "UPDATE companies SET display_name = 'Hacked' WHERE id = {0}", companyB_Id);
                    
                    rowsAffected.Should().Be(0, "RLS policy should completely hide Company B's row from the UPDATE");

                    await ctxA_Update.Database.RollbackTransactionAsync();
                }
            }
        }

        // 5. Test Cross-Tenant Insert Rejection
        // Company A attempts to insert a record into Company B's scope (using companyB_Id).
        // The WITH CHECK RLS policy should violently reject the insert with 42501 insufficient_privilege.
        await using (var ctxA_Insert = CreateContext(companyA_Id))
        {
            await ctxA_Insert.Database.BeginTransactionAsync();
            
            // Using raw SQL to bypass EF Core's knowledge of the 1:1 unique constraint on CompanySettings,
            // or simply inserting a fake row if possible. We can use a raw SQL INSERT to test database-level RLS.
            var act = async () => await ctxA_Insert.Database.ExecuteSqlRawAsync(
                "INSERT INTO company_settings (company_id, late_fee_type, late_fee_value, fiscal_year_start_month) VALUES ({0}, 'percentage', 5, 1)", companyB_Id);
            
            var ex = await act.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("42501", "RLS WITH CHECK policy should reject inserts that do not match the session company_id");

            await ctxA_Insert.Database.RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task PooledConnection_TenantLeakage_DoesNotOccur()
    {
        // Prove that setting a transaction-local context does not leak to a subsequent request
        // on the same physical connection.
        var companyA_Id = Guid.Empty;
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            await adminCtx.Database.BeginTransactionAsync();
            var compA = Company.Create(
                "Legal A", "Display A", "+962790000001", CompanyType.PropertyManagementCompany,
                "JO", DateTimeOffset.UtcNow, null, "CR1", "TAX1", "a@test.com");
            adminCtx.Set<Company>().Add(compA);
            await adminCtx.SaveChangesAsync();
            companyA_Id = compA.Id;
            await adminCtx.Database.CommitTransactionAsync();
        }

        // Request 1: Opens connection, starts transaction, gets Company A, commits.
        await using (var ctx1 = CreateContext(companyA_Id))
        {
            await ctx1.Database.BeginTransactionAsync();
            var count = await ctx1.Set<Company>().CountAsync();
            count.Should().Be(1);
            await ctx1.Database.CommitTransactionAsync();
        }

        // Request 2: Reuses the SAME connection from the pool, but lacks tenant context.
        // It should NOT see Company A.
        await using (var ctx2 = CreateContext(null))
        {
            await ctx2.Database.BeginTransactionAsync();
            // Since CompanyId is null, TenantSessionInterceptor skips SET LOCAL.
            // RLS should fail-closed. Because we use NULLIF(current_setting('app.current_company_id', true), ''),
            // the missing or empty ("") context safely evaluates to NULL.
            // The USING (id = NULL) expression evaluates to NULL (false), safely hiding all rows
            // without throwing an invalid input syntax exception.
            var count = await ctx2.Set<Company>().CountAsync();
            count.Should().Be(0, "RLS should cleanly fail closed when tenant context is missing or reverted");
            
            // Prove that an INSERT without tenant context is rejected by WITH CHECK
            var act = async () => await ctx2.Database.ExecuteSqlRawAsync(
                "INSERT INTO company_settings (company_id, late_fee_type, late_fee_value, fiscal_year_start_month) VALUES ({0}, 'percentage', 5, 1)", Guid.NewGuid());
            
            var ex = await act.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("42501", "RLS WITH CHECK should cleanly reject inserts when context is missing");
            
            await ctx2.Database.RollbackTransactionAsync();
        }
    }
}
