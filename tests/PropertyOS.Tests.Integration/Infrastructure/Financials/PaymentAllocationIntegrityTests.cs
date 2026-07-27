using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Application;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Infrastructure.Financials.Repositories;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Behaviors;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Financials;

/// <summary>
/// Verifies the two layers of over-allocation defense:
///  1. trg_payment_allocations_enforce_limits — the DB trigger backstop (direct SQL cannot
///     over-allocate an obligation or over-draw a receiving payment), and
///  2. the FOR UPDATE locking protocol in RecordPaymentAllocationCommandHandler — two
///     concurrent allocators against the same obligation serialize; exactly one wins.
/// </summary>
[Collection("Postgres collection")]
public class PaymentAllocationIntegrityTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private ServiceProvider? _serviceProvider;
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PaymentAllocationIntegrityTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    private sealed record FinancialSeed(Guid ObligationId, Guid ReceivingId, Guid SecondReceivingId, Guid SecondObligationId);

    /// <summary>
    /// Seeds a company/building/floor/apartment/tenant/lease chain plus:
    ///  - obligation installment (amount_due 1000),
    ///  - a second obligation installment (amount_due 500),
    ///  - two receiving payments (unallocated receipts) with amount_due 600 and 500.
    /// </summary>
    private async Task<FinancialSeed> SeedAsync()
    {
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var obligationId = Guid.NewGuid();
        var secondObligationId = Guid.NewGuid();
        var receivingId = Guid.NewGuid();
        var secondReceivingId = Guid.NewGuid();

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (id, full_name, email) VALUES (@uId, 'Fin User', 'fin@example.com');
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at)
                VALUES (@cId, 'FinCo', 'FinCo', '+962791234567', 'individual_owner', now(), now());
            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at)
                VALUES (@bId, @cId, 'B', 'residential', 1, now(), now());
            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
                VALUES (@fId, @cId, @bId, 1, 'Floor 1', 'regular', now(), now());
            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at)
                VALUES (@aId, @fId, @bId, @cId, '101', 'vacant', 1, 1, 100, 100, now(), now());
            INSERT INTO tenants (id, company_id, name, national_id, phone, created_at, updated_at)
                VALUES (@tId, @cId, 'T Fin', '1234567890', '+962791234567', now(), now());
            INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, legal_regime, tenant_type,
                                         start_date, end_date, monthly_rent_amount, currency, security_deposit_amount,
                                         payment_frequency, payment_due_day, status, created_at, updated_at)
                VALUES (@lcId, @cId, @bId, @aId, @tId, 'LC-FIN-1', 'standard', 'personal',
                        '2026-01-01', '2027-01-01', 1000, 'JOD', 0, 'monthly', 1, 'active', now(), now());
            INSERT INTO rent_payments (id, company_id, lease_contract_id, tenant_id, building_id, apartment_id,
                                       billing_period_start, billing_period_end, due_date, amount_due, amount_paid,
                                       currency, payment_purpose, due_date_status, created_at, updated_at)
                VALUES (@obId, @cId, @lcId, @tId, @bId, @aId,
                        '2026-03-01', '2026-04-01', '2026-03-01', 1000, 0,
                        'JOD', 'scheduled_installment', 'pending', now(), now());
            INSERT INTO rent_payments (id, company_id, lease_contract_id, tenant_id, building_id, apartment_id,
                                       billing_period_start, billing_period_end, due_date, amount_due, amount_paid,
                                       currency, payment_purpose, due_date_status, created_at, updated_at)
                VALUES (@ob2Id, @cId, @lcId, @tId, @bId, @aId,
                        '2026-04-01', '2026-05-01', '2026-04-01', 500, 0,
                        'JOD', 'scheduled_installment', 'pending', now(), now());
            INSERT INTO rent_payments (id, company_id, lease_contract_id, tenant_id, building_id, apartment_id,
                                       billing_period_start, billing_period_end, due_date, amount_due, amount_paid,
                                       currency, payment_purpose, due_date_status, created_at, updated_at)
                VALUES (@rcId, @cId, @lcId, @tId, @bId, @aId,
                        NULL, NULL, NULL, 600, 0,
                        'JOD', 'unallocated_receipt', 'pending', now(), now());
            INSERT INTO rent_payments (id, company_id, lease_contract_id, tenant_id, building_id, apartment_id,
                                       billing_period_start, billing_period_end, due_date, amount_due, amount_paid,
                                       currency, payment_purpose, due_date_status, created_at, updated_at)
                VALUES (@rc2Id, @cId, @lcId, @tId, @bId, @aId,
                        NULL, NULL, NULL, 500, 0,
                        'JOD', 'unallocated_receipt', 'pending', now(), now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("uId", _userId));
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("fId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("aId", apartmentId));
        cmd.Parameters.Add(new NpgsqlParameter("tId", tenantId));
        cmd.Parameters.Add(new NpgsqlParameter("lcId", contractId));
        cmd.Parameters.Add(new NpgsqlParameter("obId", obligationId));
        cmd.Parameters.Add(new NpgsqlParameter("ob2Id", secondObligationId));
        cmd.Parameters.Add(new NpgsqlParameter("rcId", receivingId));
        cmd.Parameters.Add(new NpgsqlParameter("rc2Id", secondReceivingId));
        await cmd.ExecuteNonQueryAsync();

        return new FinancialSeed(obligationId, receivingId, secondReceivingId, secondObligationId);
    }

    private async Task InsertAllocationRawAsync(Guid receivingId, Guid obligationId, decimal amount)
    {
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO payment_allocations (company_id, receiving_payment_id, obligation_payment_id,
                                             allocated_amount, allocation_date, allocation_status, created_at, updated_at)
            VALUES (@cId, @rcId, @obId, @amount, CURRENT_DATE, 'active', now(), now());";
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("rcId", receivingId));
        cmd.Parameters.Add(new NpgsqlParameter("obId", obligationId));
        cmd.Parameters.Add(new NpgsqlParameter("amount", amount));
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Trigger_ObligationOverAllocation_DirectSql_IsRejected()
    {
        var seed = await SeedAsync();

        // 600 of 1000 — fine.
        await InsertAllocationRawAsync(seed.ReceivingId, seed.ObligationId, 600m);

        // 500 more would take the obligation to 1100 > 1000 — the trigger must reject it,
        // even though this INSERT bypasses the application layer entirely.
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertAllocationRawAsync(seed.SecondReceivingId, seed.ObligationId, 500m));

        Assert.Equal("23514", ex.SqlState);
        Assert.Contains("Over-allocation", ex.Message);
    }

    [Fact]
    public async Task Trigger_ReceivingSideOverdraw_DirectSql_IsRejected()
    {
        var seed = await SeedAsync();

        // Receiving payment holds 600: allocate 400 to obligation 1 (fine), then 300 to
        // obligation 2 — that would draw 700 out of 600 received. Trigger must reject.
        await InsertAllocationRawAsync(seed.ReceivingId, seed.ObligationId, 400m);

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertAllocationRawAsync(seed.ReceivingId, seed.SecondObligationId, 300m));

        Assert.Equal("23514", ex.SqlState);
        Assert.Contains("Over-allocation", ex.Message);
    }

    [Fact]
    public async Task Diagnostic_ForUpdateLoad_IsTrackedAndPersists()
    {
        var seed = await SeedAsync();
        SetupDI();

        using (var scope = _serviceProvider!.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var repo = scope.ServiceProvider.GetRequiredService<IRentPaymentRepository>();

            await using var tx = await db.Database.BeginTransactionAsync();
            var rows = await repo.GetByIdsForUpdateAsync(new[] { seed.ObligationId });
            var obligation = Assert.Single(rows);

            Assert.Equal(Microsoft.EntityFrameworkCore.EntityState.Unchanged, db.Entry(obligation).State);

            obligation.UpdateAllocationSync(123m, PropertyOS.Domain.Financials.Enums.DueDateStatus.PartiallyPaid, DateTimeOffset.UtcNow, _userId);
            db.ChangeTracker.DetectChanges();
            Assert.Equal(Microsoft.EntityFrameworkCore.EntityState.Modified, db.Entry(obligation).State);

            var written = await db.SaveChangesAsync();
            Assert.True(written >= 1, $"SaveChanges wrote {written} rows");
            await tx.CommitAsync();
        }

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT amount_paid FROM rent_payments WHERE id = @obId;";
        cmd.Parameters.Add(new NpgsqlParameter("obId", seed.ObligationId));
        var value = (decimal)(await cmd.ExecuteScalarAsync() ?? 0m);
        Assert.Equal(123m, value);
    }

    [Fact]
    public async Task SingleAllocation_PersistsObligationCacheUpdate()
    {
        var seed = await SeedAsync();
        SetupDI();

        using (var scope = _serviceProvider!.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RecordPaymentAllocationCommand(
                seed.ReceivingId,
                new List<AllocationDetail> { new(seed.ObligationId, 600m) },
                DateOnly.FromDateTime(DateTime.UtcNow)));
        }

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT amount_paid, due_date_status::text FROM rent_payments WHERE id = @obId;";
        cmd.Parameters.Add(new NpgsqlParameter("obId", seed.ObligationId));
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(600m, reader.GetDecimal(0));
        // Doc §6.1 matrix: the seeded obligation's due date (2026-03-01) is past the
        // grace-adjusted boundary, so a partial payment derives 'late'.
        Assert.Equal("late", reader.GetString(1));
    }

    [Fact]
    public async Task ConcurrentAllocations_SameObligation_ExactlyOneSucceeds()
    {
        var seed = await SeedAsync();
        SetupDI();

        // Both allocators are individually valid (600 and 500 against a 1000 obligation,
        // each within its own receiving payment's funds), but together they exceed the
        // obligation. The FOR UPDATE protocol must serialize them so the loser re-reads
        // the winner's committed allocation and fails the outstanding-balance check.
        async Task<Exception?> TryAllocate(Guid receivingId, decimal amount)
        {
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new RecordPaymentAllocationCommand(
                    receivingId,
                    new List<AllocationDetail> { new(seed.ObligationId, amount) },
                    DateOnly.FromDateTime(DateTime.UtcNow)));
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var results = await Task.WhenAll(
            Task.Run(() => TryAllocate(seed.ReceivingId, 600m)),
            Task.Run(() => TryAllocate(seed.SecondReceivingId, 500m)));

        var failures = results.Where(r => r != null).ToList();
        Assert.Single(failures);

        // The loser must fail the application-layer check (or, in the extreme, the trigger).
        Assert.True(
            failures[0] is BusinessRuleException || failures[0] is DbUpdateException,
            $"Unexpected failure type: {failures[0]!.GetType().Name}: {failures[0]!.Message}");

        // The obligation must never exceed its amount_due, and exactly one allocation is active.
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT (SELECT COALESCE(SUM(allocated_amount), 0) FROM payment_allocations
                     WHERE obligation_payment_id = @obId AND allocation_status = 'active' AND deleted_at IS NULL),
                   (SELECT amount_paid FROM rent_payments WHERE id = @obId);";
        cmd.Parameters.Add(new NpgsqlParameter("obId", seed.ObligationId));
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var allocatedSum = reader.GetDecimal(0);
        var amountPaid = reader.GetDecimal(1);

        Assert.True(allocatedSum <= 1000m, $"Obligation over-allocated: {allocatedSum}");
        Assert.Equal(allocatedSum, amountPaid);
    }

    private void SetupDI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();

        var dataSource = FinancialsTestDataSource.Build(_fixture.RawConnectionString);

        services.AddScoped<AuditTransactionState>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditTransactionInterceptor>();
        services.AddScoped<TenantSessionInterceptor>();
        services.AddDbContext<PropertyOsDbContext>((sp, options) =>
        {
            FinancialsTestDataSource.ConfigureNpgsql(options, dataSource);
            var sqlLogPath = Environment.GetEnvironmentVariable("PROPERTYOS_TEST_SQL_LOG");
            if (!string.IsNullOrEmpty(sqlLogPath))
            {
                options.LogTo(
                    message => System.IO.File.AppendAllText(sqlLogPath, message + Environment.NewLine),
                    Microsoft.Extensions.Logging.LogLevel.Information);
            }
            options.AddInterceptors(
                sp.GetRequiredService<TenantSessionInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditTransactionInterceptor>());
        });

        services.AddScoped<IRentPaymentRepository, RentPaymentRepository>();

        services.AddSingleton<ITenantContext>(new FakeTenantContext { CompanyId = _companyId });
        services.AddSingleton<ICurrentUserContext>(new FakeCurrentUserContext { UserId = _userId });
        services.AddSingleton<IAuditRequestContext>(new FakeAuditRequestContext
        {
            RequestId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Source = PropertyOS.Domain.Audit.Enums.AuditSource.Api
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        _serviceProvider = services.BuildServiceProvider();
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin { get; set; }
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; }
    }

    private class FakeAuditRequestContext : IAuditRequestContext
    {
        public Guid? RequestId { get; set; }
        public Guid? CorrelationId { get; set; }
        public PropertyOS.Domain.Audit.Enums.AuditSource Source { get; set; }
    }
}
