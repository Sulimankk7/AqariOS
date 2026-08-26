using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.UtilityBills;

/// <summary>
/// PostgreSQL integration tests for Utility Bills:
///   1. Duplicate Bill Prevention (Database constraint + ON CONFLICT DO NOTHING)
///   2. Concurrent Account Claim (FOR UPDATE SKIP LOCKED mutual exclusion)
///   3. Scheduler Predicate Coverage (Filtering NextCheckAt, Bootstrap, Active, Soft-delete)
/// </summary>
[Collection("Postgres collection")]
public class UtilityBillsDatabaseIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _buildingId = Guid.NewGuid();
    private readonly Guid _apartmentId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _leaseId = Guid.NewGuid();

    public UtilityBillsDatabaseIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IServiceProvider CreateServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Production DI (DependencyInjection.cs:438) resolves IHostEnvironment during
        // EF Core AddDbContext configuration to decide whether to enable sensitive data
        // logging. In production this is always provided by WebApplication.CreateBuilder.
        // Here we use a raw ServiceCollection (no host), so we register a minimal stub.
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["UtilityBills:Electricity:Enabled"] = "true",
            ["UtilityBills:Water:Enabled"] = "true"
        }).Build();

        services.AddInfrastructureServices(config);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Minimal IHostEnvironment used by the test-only service provider.
    /// It declares "Development" so that sensitive data logging is enabled during tests
    /// (consistent with how developers run the API locally).
    /// This type lives only in the test project — no production code is touched.
    /// </summary>
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "PropertyOS.Tests.Integration";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }


    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Seed tenant, building, apartment, and lease contract for foreign key integrity
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $@"
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at, is_active)
            VALUES ('{_companyId}', 'Test Company LLC', 'Test Company', '+962790000000', 'individual_owner'::company_type_enum, now(), now(), true);

            INSERT INTO users (id, full_name, email)
            VALUES ('{_userId}', 'Test Tenant', 'tenant@test.local');

            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at)
            VALUES ('{_buildingId}', '{_companyId}', 'Building 1', 'residential'::building_type_enum, 4, now(), now());

            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES ('{Guid.NewGuid()}', '{_companyId}', '{_buildingId}', 1, 'Floor 1', 'regular'::floor_type_enum, now(), now());

            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at)
            VALUES ('{_apartmentId}', (SELECT id FROM floors WHERE building_id = '{_buildingId}' LIMIT 1), '{_buildingId}', '{_companyId}', '101', 'vacant'::occupancy_status_enum, 2, 1, 400.0, 90.0, now(), now());

            INSERT INTO tenants (id, company_id, name, national_id, phone, user_id, created_at, updated_at)
            VALUES ('{_tenantId}', '{_companyId}', 'Tenant Name', '9990001111', '+962791112233', '{_userId}', now(), now());

            INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, created_at, updated_at)
            VALUES ('{_leaseId}', '{_companyId}', '{_buildingId}', '{_apartmentId}', '{_tenantId}', 'LC-INT-001', '2026-01-01', '2026-12-31', 400.0, 'monthly'::payment_frequency_enum, 1, now(), now());

        ";
        await cmd.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. Duplicate Bill Prevention ─────────────────────────────────────────

    [Fact]
    public async Task DuplicateBillPrevention_SecondInsertReturnsFalseAndSingleRowPersisted()
    {
        var accountId = Guid.NewGuid();
        await using (var adminConn = new NpgsqlConnection(_fixture.RawConnectionString))
        {
            await adminConn.OpenAsync();
            await using var cmd = adminConn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed)
                VALUES ('{accountId}', '{_companyId}', '{_leaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-DUP-01', true, true);
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        var sp = CreateServiceProvider(_fixture.RawConnectionString);
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IUtilityBillRepository>();

        var bill1 = UtilityBill.Create(
            companyId: _companyId,
            utilityAccountId: accountId,
            utilityType: UtilityType.Electricity,
            providerExternalId: "BILL-EXT-999",
            billDate: new DateOnly(2026, 5, 1),
            dueDate: new DateOnly(2026, 5, 25),
            amount: 45.500m,
            currency: "JOD",
            isPaid: false,
            paymentStatus: UtilityBillPaymentStatus.Unpaid,
            providerReference: "REF-123",
            isFromHistoricalBackfill: false,
            discoveredAt: DateTimeOffset.UtcNow);

        var bill2 = UtilityBill.Create(
            companyId: _companyId,
            utilityAccountId: accountId,
            utilityType: UtilityType.Electricity,
            providerExternalId: "BILL-EXT-999", // Same external ID and account ID
            billDate: new DateOnly(2026, 5, 1),
            dueDate: new DateOnly(2026, 5, 25),
            amount: 45.500m,
            currency: "JOD",
            isPaid: false,
            paymentStatus: UtilityBillPaymentStatus.Unpaid,
            providerReference: "REF-123",
            isFromHistoricalBackfill: false,
            discoveredAt: DateTimeOffset.UtcNow);

        // Act
        var firstResult = await repo.InsertIfNewAsync(bill1);
        var secondResult = await repo.InsertIfNewAsync(bill2);

        // Assert
        firstResult.Should().BeTrue("first insert should successfully create a new record");
        secondResult.Should().BeFalse("second insert with identical (utility_account_id, provider_external_id) must be ignored via ON CONFLICT DO NOTHING");

        // Verify exactly 1 row exists in PostgreSQL
        var count = await context.UtilityBills.CountAsync(b => b.UtilityAccountId == accountId);
        count.Should().Be(1);
    }

    // ── Nullable Raw-SQL Parameters ─────────────────────────────────────

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task InsertIfNewAsync_NullableParametersPersistAsDatabaseNullAndPreserveDuplicatePrevention(
        bool dueDateIsNull,
        bool providerReferenceIsNull)
    {
        var accountId = Guid.NewGuid();
        await using (var adminConn = new NpgsqlConnection(_fixture.RawConnectionString))
        {
            await adminConn.OpenAsync();
            await using var cmd = adminConn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed)
                VALUES ('{accountId}', '{_companyId}', '{_leaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-NULL-{accountId:N}', true, true);
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        var serviceProvider = CreateServiceProvider(_fixture.RawConnectionString);
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUtilityBillRepository>();
        var providerExternalId = $"BILL-NULL-{accountId:N}";

        UtilityBill CreateBill() => UtilityBill.Create(
            companyId: _companyId,
            utilityAccountId: accountId,
            utilityType: UtilityType.Electricity,
            providerExternalId: providerExternalId,
            billDate: new DateOnly(2026, 6, 1),
            dueDate: dueDateIsNull ? null : new DateOnly(2026, 6, 25),
            amount: 31.750m,
            currency: "JOD",
            isPaid: true,
            paymentStatus: UtilityBillPaymentStatus.Paid,
            providerReference: providerReferenceIsNull ? null : "REF-NULLABLE-TEST",
            isFromHistoricalBackfill: true,
            discoveredAt: DateTimeOffset.UtcNow);

        var firstResult = await repository.InsertIfNewAsync(CreateBill());
        var duplicateResult = await repository.InsertIfNewAsync(CreateBill());

        firstResult.Should().BeTrue();
        duplicateResult.Should().BeFalse(
            "nullable parameter binding must not change ON CONFLICT duplicate prevention");

        await using var verificationConnection = new NpgsqlConnection(_fixture.RawConnectionString);
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = """
            SELECT due_date IS NULL,
                   provider_reference IS NULL,
                   count(*) OVER ()
            FROM utility_bills
            WHERE utility_account_id = @accountId
              AND provider_external_id = @providerExternalId
            """;
        verificationCommand.Parameters.AddWithValue("accountId", accountId);
        verificationCommand.Parameters.AddWithValue("providerExternalId", providerExternalId);

        await using var reader = await verificationCommand.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue("the bill must be persisted");
        reader.GetBoolean(0).Should().Be(dueDateIsNull);
        reader.GetBoolean(1).Should().Be(providerReferenceIsNull);
        reader.GetInt64(2).Should().Be(1,
            "the duplicate insert must leave exactly one PostgreSQL row");
        (await reader.ReadAsync()).Should().BeFalse();
    }

    // ── 2. Concurrent Utility Account Claim (FOR UPDATE SKIP LOCKED) ──────────

    [Fact]
    public async Task ConcurrentUtilityAccountClaim_TwoWorkersRace_OnlyOneAcquiresLock()
    {
        var accountId = Guid.NewGuid();
        await using (var adminConn = new NpgsqlConnection(_fixture.RawConnectionString))
        {
            await adminConn.OpenAsync();
            await using var cmd = adminConn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed)
                VALUES ('{accountId}', '{_companyId}', '{_leaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-CONC-01', true, true);
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        var staleThreshold = DateTimeOffset.UtcNow.AddMinutes(-10);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var sp1 = CreateServiceProvider(_fixture.RawConnectionString);
        var sp2 = CreateServiceProvider(_fixture.RawConnectionString);
        using var sp1Lifetime = (IDisposable)sp1;
        using var sp2Lifetime = (IDisposable)sp2;
        using var scope1 = sp1.CreateScope();
        using var scope2 = sp2.CreateScope();

        var context1 = scope1.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var repo1 = scope1.ServiceProvider.GetRequiredService<IUtilityAccountRepository>();
        var repo2 = scope2.ServiceProvider.GetRequiredService<IUtilityAccountRepository>();

        UtilityAccount? worker1Result = null;
        UtilityAccount? worker2Result = null;

        var worker1Strategy = context1.Database.CreateExecutionStrategy();
        await worker1Strategy.ExecuteAsync(async () =>
        {
            // Keep Worker 1's transaction open so its row lock remains held while Worker 2
            // attempts the same claim through a different DbContext and database connection.
            await using var tx1 = await context1.Database.BeginTransactionAsync(timeoutCts.Token);
            worker1Result = await repo1.TryClaimForSyncAsync(
                accountId, staleThreshold, timeoutCts.Token);

            var worker2Strategy = context2.Database.CreateExecutionStrategy();
            await worker2Strategy.ExecuteAsync(async () =>
            {
                await using var tx2 = await context2.Database.BeginTransactionAsync(timeoutCts.Token);
                worker2Result = await repo2.TryClaimForSyncAsync(
                    accountId, staleThreshold, timeoutCts.Token);
                await tx2.RollbackAsync(timeoutCts.Token);
            });

            await tx1.CommitAsync(timeoutCts.Token);
        });

        // Assert
        worker1Result.Should().NotBeNull("Worker 1 must successfully claim the unencumbered account");
        worker1Result!.Id.Should().Be(accountId);

        worker2Result.Should().BeNull("Worker 2 must receive null immediately because Worker 1 holds the lock (FOR UPDATE SKIP LOCKED)");
    }

    // ── 3. Scheduler Predicate Coverage ──────────────────────────────────────

    [Fact]
    public async Task SchedulerPredicateCoverage_ExcludesFutureInactiveDeletedAndUnbootstrappedAccounts()
    {
        var now = DateTimeOffset.UtcNow;
        var pastDue = now.AddHours(-1);
        var futureDue = now.AddDays(2);

        var validDueId = Guid.NewGuid();
        var futureId = Guid.NewGuid();
        var unbootstrappedId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        var futureLeaseId = Guid.NewGuid();
        var unbootstrappedLeaseId = Guid.NewGuid();
        var inactiveLeaseId = Guid.NewGuid();
        var deletedLeaseId = Guid.NewGuid();

        await using (var adminConn = new NpgsqlConnection(_fixture.RawConnectionString))
        {
            await adminConn.OpenAsync();
            await using var cmd = adminConn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, created_at, updated_at)
                VALUES
                    ('{futureLeaseId}', '{_companyId}', '{_buildingId}', '{_apartmentId}', '{_tenantId}', 'LC-INT-FUTURE', '2026-01-01', '2026-12-31', 400.0, 'monthly'::payment_frequency_enum, 1, now(), now()),
                    ('{unbootstrappedLeaseId}', '{_companyId}', '{_buildingId}', '{_apartmentId}', '{_tenantId}', 'LC-INT-UNBOOT', '2026-01-01', '2026-12-31', 400.0, 'monthly'::payment_frequency_enum, 1, now(), now()),
                    ('{inactiveLeaseId}', '{_companyId}', '{_buildingId}', '{_apartmentId}', '{_tenantId}', 'LC-INT-INACTIVE', '2026-01-01', '2026-12-31', 400.0, 'monthly'::payment_frequency_enum, 1, now(), now()),
                    ('{deletedLeaseId}', '{_companyId}', '{_buildingId}', '{_apartmentId}', '{_tenantId}', 'LC-INT-DELETED', '2026-01-01', '2026-12-31', 400.0, 'monthly'::payment_frequency_enum, 1, now(), now());

                -- 1. Valid due account (Should be selected)
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed, next_check_at)
                VALUES ('{validDueId}', '{_companyId}', '{_leaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-VALID', true, true, '{pastDue:O}');

                -- 2. Future due account (Should NOT be selected)
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed, next_check_at)
                VALUES ('{futureId}', '{_companyId}', '{futureLeaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-FUTURE', true, true, '{futureDue:O}');

                -- 3. Unbootstrapped account (Should NOT be selected by main scheduler)
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed, next_check_at)
                VALUES ('{unbootstrappedId}', '{_companyId}', '{unbootstrappedLeaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-UNBOOT', true, false, '{pastDue:O}');

                -- 4. Inactive account (Should NOT be selected)
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed, next_check_at)
                VALUES ('{inactiveId}', '{_companyId}', '{inactiveLeaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-INACTIVE', false, true, '{pastDue:O}');

                -- 5. Soft-deleted account (Should NOT be selected)
                INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed, next_check_at, deleted_at)
                VALUES ('{deletedId}', '{_companyId}', '{deletedLeaseId}', '{_tenantId}', '{_apartmentId}', 'electricity'::utility_type_enum, 'ELEC-DELETED', true, true, '{pastDue:O}', now());
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        var sp = CreateServiceProvider(_fixture.RawConnectionString);
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUtilityAccountRepository>();

        // Act
        var dueIds = await repo.GetDueAccountIdsAsync(
            UtilityType.Electricity,
            asOf: now,
            batchSize: 10,
            afterId: null);

        // Assert
        dueIds.Should().Contain(validDueId, "valid, active, bootstrapped, past-due account must be selected");
        dueIds.Should().NotContain(futureId, "future-due account must be excluded by NextCheckAt filter");
        dueIds.Should().NotContain(unbootstrappedId, "unbootstrapped account must be excluded by scheduler index filter");
        dueIds.Should().NotContain(inactiveId, "inactive account must be excluded by IsActive filter");
        dueIds.Should().NotContain(deletedId, "soft-deleted account must be excluded by DeletedAt filter");
    }
}
