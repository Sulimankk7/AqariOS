using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PropertyOS.Application;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;
using PropertyOS.Application.UtilityBills.Queries.GetMyUtilityBills;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.UtilityBills.Jobs;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.UtilityBills;

[Collection("Postgres collection")]
public sealed class UtilityBillsSyncTransactionIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public UtilityBillsSyncTransactionIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Sync_ClaimCommitsBeforeProvider_ProviderRunsWithoutTransaction_AndSuccessCommitsAtomically()
    {
        var seed = await SeedAccountAsync();
        var providerEntered = NewSignal();
        var releaseProvider = NewSignal();

        using var serviceProvider = CreateServiceProvider(async (hasActiveTransaction, _, cancellationToken) =>
        {
            hasActiveTransaction.Should().BeFalse(
                "provider I/O must execute after the claim transaction commits");

            var claim = await ReadClaimAsync(seed.AccountId, cancellationToken);
            claim.ClaimedAt.Should().NotBeNull("the committed claim must be visible to another connection");
            claim.ClaimedByJobRunId.Should().NotBeNullOrWhiteSpace();

            providerEntered.TrySetResult();
            await releaseProvider.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            return ProviderBillFetchResult.Success(new[]
            {
                NewBill("SYNC-SUCCESS-1")
            });
        });

        using var dispatchScope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        var sender = dispatchScope.ServiceProvider.GetRequiredService<ISender>();
        var syncTask = sender.Send(new SyncUtilityAccountCommand(seed.AccountId));

        await providerEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // A different connection can acquire the row lock while provider I/O is blocked,
        // proving the short claim transaction released its PostgreSQL lock before HTTP work.
        await using (var lockConnection = new NpgsqlConnection(_fixture.RawConnectionString))
        {
            await lockConnection.OpenAsync();
            await using var lockTransaction = await lockConnection.BeginTransactionAsync();
            await using var lockCommand = lockConnection.CreateCommand();
            lockCommand.Transaction = lockTransaction;
            lockCommand.CommandText = "SELECT id FROM utility_accounts WHERE id = @id FOR UPDATE NOWAIT";
            lockCommand.Parameters.AddWithValue("id", seed.AccountId);
            (await lockCommand.ExecuteScalarAsync()).Should().Be(seed.AccountId);
            await lockTransaction.RollbackAsync();
        }

        releaseProvider.TrySetResult();
        await syncTask.WaitAsync(TimeSpan.FromSeconds(10));

        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                a.sync_status::text,
                a.claimed_at IS NULL AND a.claimed_by_job_run_id IS NULL,
                a.last_known_bill_date,
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id),
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id AND b.notification_sent_at IS NOT NULL),
                (SELECT count(*) FROM notifications n WHERE n.company_id = a.company_id AND n.recipient_user_id = @userId),
                (SELECT count(*) FROM notification_deliveries d JOIN notifications n ON n.id = d.notification_id WHERE n.company_id = a.company_id AND n.recipient_user_id = @userId)
            FROM utility_accounts a
            WHERE a.id = @accountId
            """;
        command.Parameters.AddWithValue("accountId", seed.AccountId);
        command.Parameters.AddWithValue("userId", seed.UserId);

        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be("synced");
        reader.GetBoolean(1).Should().BeTrue("the claim must be released in the persistence transaction");
        reader.GetFieldValue<DateOnly>(2).Should().Be(new DateOnly(2026, 5, 1));
        reader.GetInt64(3).Should().Be(1);
        reader.GetInt64(4).Should().Be(1, "NotificationSentAt must commit with the bill and account state");
        reader.GetInt64(5).Should().Be(1);
        reader.GetInt64(6).Should().Be(1);
    }

    [Fact]
    public async Task Bootstrap_FullHistoryPersistsAllSuppressesNotifications_IsIdempotent_AndNormalSyncNotifiesNewUnpaidBill()
    {
        var seed = await SeedAccountAsync(includeUtilityAccount: false);
        var historicalBills = Enumerable.Range(1, 8)
            .Select(month => NewHistoricalBill(
                month,
                isPaid: month <= 5,
                includeDueDate: month != 1))
            .ToList();
        var observedSinceDates = new List<DateOnly?>();
        var scheduler = new CapturingUtilityBillsJobScheduler();

        using var serviceProvider = CreateServiceProvider(
            (_, sinceDate, _) =>
            {
                observedSinceDates.Add(sinceDate);
                return Task.FromResult(sinceDate is null
                    ? ProviderBillFetchResult.Success(historicalBills)
                    : ProviderBillFetchResult.Success(new[]
                    {
                        NewBill("NEW-UNPAID-2026-09", new DateOnly(2026, 9, 1))
                    }));
            },
            scheduler,
            seed.UserId);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var linkScope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        var accountId = await linkScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new LinkUtilityAccountCommand(
                seed.LeaseId,
                UtilityType.Electricity,
                "0000000001"),
            timeout.Token);

        scheduler.BootstrapAccountIds.Should().ContainSingle()
            .Which.Should().Be(accountId,
                "linking must enqueue bootstrap only after the account transaction commits");

        var bootstrapJob = serviceProvider.GetRequiredService<BootstrapUtilityAccountJob>();
        await bootstrapJob.ExecuteAsync(accountId, timeout.Token);

        var firstBootstrap = await ReadPersistenceSnapshotAsync(
            accountId, seed.CompanyId, seed.UserId, timeout.Token);
        firstBootstrap.BillCount.Should().Be(8);
        firstBootstrap.PaidCount.Should().Be(5);
        firstBootstrap.UnpaidCount.Should().Be(3);
        firstBootstrap.HistoricalCount.Should().Be(8);
        firstBootstrap.NotificationCount.Should().Be(0);
        firstBootstrap.DeliveryCount.Should().Be(0);
        firstBootstrap.HistoricalBootstrapCompleted.Should().BeTrue();
        firstBootstrap.LastKnownBillDate.Should().Be(new DateOnly(2026, 8, 1));
        firstBootstrap.NullDueDateCount.Should().Be(1,
            "an IDECO-shaped bill without a due date must persist as PostgreSQL NULL");
        firstBootstrap.SyncStatus.Should().Be("synced");
        firstBootstrap.ClaimReleased.Should().BeTrue(
            "phase two must commit and release the bootstrap claim");

        await bootstrapJob.ExecuteAsync(accountId, timeout.Token);

        var repeatedBootstrap = await ReadPersistenceSnapshotAsync(
            accountId, seed.CompanyId, seed.UserId, timeout.Token);
        repeatedBootstrap.BillCount.Should().Be(8,
            "the database uniqueness constraint must suppress repeated history");
        repeatedBootstrap.HistoricalCount.Should().Be(8);
        repeatedBootstrap.NotificationCount.Should().Be(0);
        repeatedBootstrap.DeliveryCount.Should().Be(0);

        using var normalScope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        await normalScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new SyncUtilityAccountCommand(accountId, IsBootstrapRun: false),
            timeout.Token);

        var normalSync = await ReadPersistenceSnapshotAsync(
            accountId, seed.CompanyId, seed.UserId, timeout.Token);
        normalSync.BillCount.Should().Be(9);
        normalSync.PaidCount.Should().Be(5);
        normalSync.UnpaidCount.Should().Be(4);
        normalSync.HistoricalCount.Should().Be(8);
        normalSync.NotifiedBillCount.Should().Be(1);
        normalSync.NotificationCount.Should().Be(1);
        normalSync.DeliveryCount.Should().Be(1);
        normalSync.LastKnownBillDate.Should().Be(new DateOnly(2026, 9, 1));

        observedSinceDates.Should().Equal(
            null,
            null,
            new DateOnly(2026, 8, 1));

        var queryHandler = new GetMyUtilityBillsQueryHandler(
            normalScope.ServiceProvider.GetRequiredService<IApplicationDbContext>(),
            normalScope.ServiceProvider.GetRequiredService<ITenantContext>(),
            new FixedCurrentUserContext(seed.UserId));
        var storedBills = await queryHandler.Handle(
            new GetMyUtilityBillsQuery(PageSize: 50), timeout.Token);

        storedBills.Items.Should().HaveCount(9,
            "the tenant bills endpoint reads the complete persisted history");
        storedBills.Items.Count(bill => bill.IsPaid).Should().Be(5);
        storedBills.Items.Count(bill => !bill.IsPaid).Should().Be(4);
    }

    [Fact]
    public async Task Sync_SecondPhasePersistenceFailure_RollsBackTrackedStateAndPreservesDuplicateProtection()
    {
        var seed = await SeedAccountAsync(existingProviderExternalId: "KNOWN-BILL");
        var invalidExternalId = new string('X', 256);

        using var serviceProvider = CreateServiceProvider((_, _, _) => Task.FromResult(
            ProviderBillFetchResult.Success(new[]
            {
                NewBill("KNOWN-BILL"),
                NewBill(invalidExternalId, new DateOnly(2026, 5, 2))
            })));

        using var scope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await FluentActions.Invoking(() => sender.Send(new SyncUtilityAccountCommand(seed.AccountId)))
            .Should().ThrowAsync<PostgresException>();

        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                sync_status::text,
                claimed_at IS NOT NULL AND claimed_by_job_run_id IS NOT NULL,
                last_known_bill_date,
                (SELECT count(*) FROM utility_bills WHERE utility_account_id = @id),
                (SELECT count(*) FROM notifications WHERE company_id = @companyId)
            FROM utility_accounts WHERE id = @id
            """;
        command.Parameters.AddWithValue("id", seed.AccountId);
        command.Parameters.AddWithValue("companyId", seed.CompanyId);

        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be("syncing", "the already-committed claim is not part of phase two");
        reader.GetBoolean(1).Should().BeTrue();
        reader.IsDBNull(2).Should().BeTrue("phase-two account changes must roll back");
        reader.GetInt64(3).Should().Be(1, "the existing duplicate remains and no partial bill is committed");
        reader.GetInt64(4).Should().Be(0);

        // The existing stale-claim invariant makes the committed claim recoverable.
        using var recoveryScope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        var db = recoveryScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var recovered = await db.ExecuteInTransactionAsync(async ct =>
        {
            var repo = recoveryScope.ServiceProvider.GetRequiredService<IUtilityAccountRepository>();
            return await repo.TryClaimForSyncAsync(seed.AccountId, DateTimeOffset.UtcNow.AddMinutes(1), ct);
        });
        recovered.Should().NotBeNull("a stale claim must remain reclaimable through the production repository");
    }

    [Fact]
    public async Task Sync_ProviderFailure_PersistsBackoffAndReleasesClaim()
    {
        var seed = await SeedAccountAsync();
        using var serviceProvider = CreateServiceProvider((_, _, _) => Task.FromResult(
            ProviderBillFetchResult.Failure("HTTP_ERROR", "Provider unavailable")));

        using var scope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new SyncUtilityAccountCommand(seed.AccountId));

        var state = await ReadAccountStateAsync(seed.AccountId);
        state.SyncStatus.Should().Be("provider_error");
        state.ConsecutiveFailureCount.Should().Be(1);
        state.LastAttemptedSyncAt.Should().NotBeNull();
        state.NextCheckAt.Should().BeAfter(state.LastAttemptedSyncAt!.Value);
        state.ClaimedAt.Should().BeNull();
        state.ClaimedByJobRunId.Should().BeNull();
    }

    [Fact]
    public async Task Sync_ProviderCancellation_PersistsFailureAndDoesNotLeaveClaimedAccount()
    {
        var seed = await SeedAccountAsync();
        using var serviceProvider = CreateServiceProvider((_, _, cancellationToken) =>
            Task.FromException<ProviderBillFetchResult>(new OperationCanceledException(cancellationToken)));

        using var scope = CreateCompanyScope(serviceProvider, seed.CompanyId);
        await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new SyncUtilityAccountCommand(seed.AccountId));

        var state = await ReadAccountStateAsync(seed.AccountId);
        state.SyncStatus.Should().Be("provider_error");
        state.ConsecutiveFailureCount.Should().Be(1);
        state.ClaimedAt.Should().BeNull();
        state.ClaimedByJobRunId.Should().BeNull();
    }

    private ServiceProvider CreateServiceProvider(
        Func<bool, DateOnly?, CancellationToken, Task<ProviderBillFetchResult>> fetch,
        IUtilityBillsJobScheduler? jobScheduler = null,
        Guid? currentUserId = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _fixture.RawConnectionString,
                ["UtilityBills:StaleClaimThresholdMinutes"] = "10",
                ["UtilityBills:Electricity:Enabled"] = "true",
                ["UtilityBills:Electricity:ProviderTimeoutSeconds"] = "30",
                ["UtilityBills:Electricity:RetryHoursWithinWindow"] = "6",
                ["UtilityBills:Electricity:BillingWindowStartHour"] = "7",
                ["UtilityBills:Electricity:MaxConsecutiveFailuresBeforeSkip"] = "5",
                ["UtilityBills:Electricity:BackoffBaseMinutes"] = "30",
                ["UtilityBills:Electricity:MaxBackoffHours"] = "24",
                ["UtilityBills:Water:Enabled"] = "true"
            }).Build();

        services.AddApplicationServices(configuration);
        services.AddInfrastructureServices(configuration);
        if (currentUserId.HasValue)
        {
            services.RemoveAll<ICurrentUserContext>();
            services.AddSingleton<ICurrentUserContext>(
                new FixedCurrentUserContext(currentUserId.Value));
        }
        services.RemoveAll<IUtilityBillingProvider>();
        services.AddScoped<IUtilityBillingProvider>(serviceProvider =>
            new ControllableElectricityProvider(
                fetch,
                serviceProvider.GetRequiredService<PropertyOsDbContext>()));
        if (jobScheduler is not null)
        {
            services.RemoveAll<IUtilityBillsJobScheduler>();
            services.AddSingleton(jobScheduler);
        }
        return services.BuildServiceProvider();
    }

    private static IServiceScope CreateCompanyScope(ServiceProvider serviceProvider, Guid companyId)
    {
        var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ISystemTenantContextSetter>().SetCompanyScope(companyId);
        return scope;
    }

    private async Task<Seed> SeedAccountAsync(
        string? existingProviderExternalId = null,
        bool historicalBootstrapCompleted = true,
        bool includeUtilityAccount = true)
    {
        var seed = new Seed(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at, is_active)
            VALUES (@companyId, 'Utility Sync Test LLC', 'Utility Sync Test', '+962790000000', 'individual_owner'::company_type_enum, now(), now(), true);
            INSERT INTO users (id, full_name, email) VALUES (@userId, 'Utility Tenant', @email);
            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at)
            VALUES (@buildingId, @companyId, 'Building', 'residential'::building_type_enum, 1, now(), now());
            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES (@floorId, @companyId, @buildingId, 1, 'Floor 1', 'regular'::floor_type_enum, now(), now());
            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at)
            VALUES (@apartmentId, @floorId, @buildingId, @companyId, '101', 'vacant'::occupancy_status_enum, 2, 1, 400, 90, now(), now());
            INSERT INTO tenants (id, company_id, name, national_id, phone, user_id, created_at, updated_at)
            VALUES (@tenantId, @companyId, 'Utility Tenant', @nationalId, '+962791112233', @userId, now(), now());
            INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, created_at, updated_at)
            VALUES (@leaseId, @companyId, @buildingId, @apartmentId, @tenantId, @contractNumber, '2026-01-01', '2026-12-31', 400, 'monthly'::payment_frequency_enum, 1, now(), now());
            INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed, next_check_at)
            SELECT @accountId, @companyId, @leaseId, @tenantId, @apartmentId, 'electricity'::utility_type_enum, @accountNumber, true, @historicalBootstrapCompleted, now()
            WHERE @includeUtilityAccount;
            """;
        command.Parameters.AddWithValue("companyId", seed.CompanyId);
        command.Parameters.AddWithValue("userId", seed.UserId);
        command.Parameters.AddWithValue("email", $"utility-{seed.UserId:N}@test.local");
        command.Parameters.AddWithValue("buildingId", seed.BuildingId);
        command.Parameters.AddWithValue("floorId", seed.FloorId);
        command.Parameters.AddWithValue("apartmentId", seed.ApartmentId);
        command.Parameters.AddWithValue("tenantId", seed.TenantId);
        command.Parameters.AddWithValue("nationalId", seed.TenantId.ToString("N")[..10]);
        command.Parameters.AddWithValue("leaseId", seed.LeaseId);
        command.Parameters.AddWithValue("contractNumber", $"LC-{seed.LeaseId:N}");
        command.Parameters.AddWithValue("accountId", seed.AccountId);
        command.Parameters.AddWithValue("accountNumber", $"ELEC-{seed.AccountId:N}");
        command.Parameters.AddWithValue(
            "historicalBootstrapCompleted", historicalBootstrapCompleted);
        command.Parameters.AddWithValue("includeUtilityAccount", includeUtilityAccount);
        await command.ExecuteNonQueryAsync();

        if (existingProviderExternalId is not null)
        {
            await using var billCommand = connection.CreateCommand();
            billCommand.CommandText = """
                INSERT INTO utility_bills (id, company_id, utility_account_id, utility_type, provider_external_id, bill_date, amount, currency, is_paid, payment_status, is_from_historical_backfill, discovered_at, created_at, updated_at)
                VALUES (@id, @companyId, @accountId, 'electricity'::utility_type_enum, @externalId, '2026-05-01', 20, 'JOD', false, 'unpaid'::utility_bill_status_enum, false, now(), now(), now())
                """;
            billCommand.Parameters.AddWithValue("id", Guid.NewGuid());
            billCommand.Parameters.AddWithValue("companyId", seed.CompanyId);
            billCommand.Parameters.AddWithValue("accountId", seed.AccountId);
            billCommand.Parameters.AddWithValue("externalId", existingProviderExternalId);
            await billCommand.ExecuteNonQueryAsync();
        }

        return seed;
    }

    private async Task<ClaimState> ReadClaimAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT claimed_at, claimed_by_job_run_id FROM utility_accounts WHERE id = @id";
        command.Parameters.AddWithValue("id", accountId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        (await reader.ReadAsync(cancellationToken)).Should().BeTrue();
        return new ClaimState(
            reader.IsDBNull(0) ? null : reader.GetFieldValue<DateTimeOffset>(0),
            reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    private async Task<AccountState> ReadAccountStateAsync(Guid accountId)
    {
        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT sync_status::text, consecutive_failure_count, last_attempted_sync_at,
                   next_check_at, claimed_at, claimed_by_job_run_id
            FROM utility_accounts WHERE id = @id
            """;
        command.Parameters.AddWithValue("id", accountId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return new AccountState(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    private async Task<PersistenceSnapshot> ReadPersistenceSnapshotAsync(
        Guid accountId,
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id),
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id AND b.is_paid),
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id AND NOT b.is_paid),
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id AND b.is_from_historical_backfill),
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id AND b.due_date IS NULL),
                (SELECT count(*) FROM utility_bills b WHERE b.utility_account_id = a.id AND b.notification_sent_at IS NOT NULL),
                (SELECT count(*) FROM notifications n WHERE n.company_id = @companyId AND n.recipient_user_id = @userId),
                (SELECT count(*) FROM notification_deliveries d JOIN notifications n ON n.id = d.notification_id WHERE n.company_id = @companyId AND n.recipient_user_id = @userId),
                a.historical_bootstrap_completed,
                a.last_known_bill_date,
                a.sync_status::text,
                a.claimed_at IS NULL AND a.claimed_by_job_run_id IS NULL
            FROM utility_accounts a
            WHERE a.id = @accountId
            """;
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("companyId", companyId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        (await reader.ReadAsync(cancellationToken)).Should().BeTrue();
        return new PersistenceSnapshot(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5),
            reader.GetInt64(6),
            reader.GetInt64(7),
            reader.GetBoolean(8),
            reader.GetFieldValue<DateOnly>(9),
            reader.GetString(10),
            reader.GetBoolean(11));
    }

    private static ProviderBillRecord NewBill(string externalId, DateOnly? billDate = null) => new(
        externalId,
        billDate ?? new DateOnly(2026, 5, 1),
        new DateOnly(2026, 5, 20),
        45.50m,
        "JOD",
        false,
        UtilityBillPaymentStatus.Unpaid,
        "REF-UTILITY-SYNC");

    private static ProviderBillRecord NewHistoricalBill(
        int month,
        bool isPaid,
        bool includeDueDate = true) => new(
        $"HISTORICAL-2026-{month:00}",
        new DateOnly(2026, month, 1),
        includeDueDate ? new DateOnly(2026, month, 20) : null,
        20m + month,
        "JOD",
        isPaid,
        isPaid ? UtilityBillPaymentStatus.Paid : UtilityBillPaymentStatus.Unpaid,
        $"FIXTURE-2026-{month:00}");

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ControllableElectricityProvider : IUtilityBillingProvider
    {
        private readonly Func<bool, DateOnly?, CancellationToken, Task<ProviderBillFetchResult>> _fetch;
        private readonly PropertyOsDbContext _dbContext;

        public ControllableElectricityProvider(
            Func<bool, DateOnly?, CancellationToken, Task<ProviderBillFetchResult>> fetch,
            PropertyOsDbContext dbContext)
        {
            _fetch = fetch;
            _dbContext = dbContext;
        }

        public UtilityType ProviderType => UtilityType.Electricity;

        public Task<ProviderBillFetchResult> FetchBillsAsync(
            string accountNumber,
            string? meterNumber,
            DateOnly? sinceDate,
            CancellationToken cancellationToken = default) =>
            _fetch(_dbContext.Database.CurrentTransaction is not null, sinceDate, cancellationToken);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "PropertyOS.Tests.Integration";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class FixedCurrentUserContext : ICurrentUserContext
    {
        public FixedCurrentUserContext(Guid userId) => UserId = userId;

        public Guid? UserId { get; }
    }

    private sealed class CapturingUtilityBillsJobScheduler : IUtilityBillsJobScheduler
    {
        public List<Guid> BootstrapAccountIds { get; } = new();

        public Task EnqueueBootstrapAsync(
            Guid utilityAccountId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BootstrapAccountIds.Add(utilityAccountId);
            return Task.CompletedTask;
        }

        public Task EnqueueSyncAsync(
            Guid utilityAccountId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed record Seed(
        Guid CompanyId,
        Guid UserId,
        Guid BuildingId,
        Guid FloorId,
        Guid ApartmentId,
        Guid TenantId,
        Guid LeaseId)
    {
        public Guid AccountId { get; } = Guid.NewGuid();
    }

    private sealed record ClaimState(DateTimeOffset? ClaimedAt, string? ClaimedByJobRunId);

    private sealed record AccountState(
        string SyncStatus,
        int ConsecutiveFailureCount,
        DateTimeOffset? LastAttemptedSyncAt,
        DateTimeOffset NextCheckAt,
        DateTimeOffset? ClaimedAt,
        string? ClaimedByJobRunId);

    private sealed record PersistenceSnapshot(
        long BillCount,
        long PaidCount,
        long UnpaidCount,
        long HistoricalCount,
        long NullDueDateCount,
        long NotifiedBillCount,
        long NotificationCount,
        long DeliveryCount,
        bool HistoricalBootstrapCompleted,
        DateOnly LastKnownBillDate,
        string SyncStatus,
        bool ClaimReleased);
}
