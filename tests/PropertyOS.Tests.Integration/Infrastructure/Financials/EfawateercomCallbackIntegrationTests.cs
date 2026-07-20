using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Financials;

[Collection("Postgres collection")]
public class EfawateercomCallbackIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private Guid _companyId;
    private Guid _buildingId;
    private Guid _apartmentId;
    private Guid _tenantId;
    private Guid _rentPaymentId;
    private Guid _leaseContractId;
    private Guid _userId;

    public EfawateercomCallbackIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _companyId = Guid.NewGuid();
        _buildingId = Guid.NewGuid();
        _apartmentId = Guid.NewGuid();
        _tenantId = Guid.NewGuid();
        _rentPaymentId = Guid.NewGuid();
        _leaseContractId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var floorId = Guid.NewGuid();

        // Seed basic dependencies directly in PG
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (id, full_name, email) 
            VALUES (@uId, 'Test User', 'test@example.com');

            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
            VALUES (@cId, 'Integration Co', 'Integration', '+962790000000', 'individual_owner', now(), now());

            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at) 
            VALUES (@bId, @cId, 'Building 1', 'residential', 1, now(), now());

            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at) 
            VALUES (@fId, @cId, @bId, 1, 'Floor 1', 'regular', now(), now());

            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at) 
            VALUES (@aId, @fId, @bId, @cId, '101', 'occupied', 2, 2, 500, 100, now(), now());

            INSERT INTO tenants (id, company_id, name, national_id, phone, created_at, updated_at) 
            VALUES (@tId, @cId, 'Tenant Name', '1234567890', '+962790000000', now(), now());

            INSERT INTO lease_contracts (id, company_id, tenant_id, building_id, apartment_id, contract_number, start_date, end_date, monthly_rent_amount, status, created_at, updated_at)
            VALUES (@lId, @cId, @tId, @bId, @aId, 'LC-1001', '2026-01-01', '2026-12-31', 500.00, 'active', now(), now());

            INSERT INTO rent_payments (id, company_id, lease_contract_id, tenant_id, building_id, apartment_id, payment_purpose, amount_due, amount_paid, currency, billing_period_start, billing_period_end, due_date, due_date_status, created_at, updated_at)
            VALUES (@rId, @cId, @lId, @tId, @bId, @aId, 'scheduled_installment', 500.00, 0.00, 'JOD', '2026-01-01', '2026-02-01', '2026-01-15', 'pending', now(), now());

            INSERT INTO company_receipt_sequences (id, company_id, prefix, padding_length, reset_policy, current_number, last_reset_at, created_at, updated_at)
            VALUES (@sId, @cId, 'REC-', 5, 'never', 0, NULL, now(), now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("uId", _userId));
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", _buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("fId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("aId", _apartmentId));
        cmd.Parameters.Add(new NpgsqlParameter("tId", _tenantId));
        cmd.Parameters.Add(new NpgsqlParameter("lId", _leaseContractId));
        cmd.Parameters.Add(new NpgsqlParameter("rId", _rentPaymentId));
        cmd.Parameters.Add(new NpgsqlParameter("sId", Guid.NewGuid()));
        await cmd.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; }

        public FakeCurrentUserContext(Guid userId)
        {
            UserId = userId;
        }
    }

    private class FakeMediator : IMediator
    {
        private readonly PropertyOsDbContext _dbContext;
        private readonly Guid _userId;

        public FakeMediator(PropertyOsDbContext dbContext, Guid userId)
        {
            _dbContext = dbContext;
            _userId = userId;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is RecordPaymentAllocationCommand allocationCmd)
            {
                // Simulate RecordPaymentAllocationCommandHandler behavior:
                // Find receiving and obligation payments, record allocation, sync amount paid
                var receiving = await _dbContext.RentPayments.FindAsync(new object[] { allocationCmd.ReceivingPaymentId }, cancellationToken);
                var allocation = allocationCmd.Allocations.First();
                var obligation = await _dbContext.RentPayments.FindAsync(new object[] { allocation.ObligationPaymentId }, cancellationToken);

                if (receiving != null && obligation != null)
                {
                    // Create payment allocation record
                    var pa = PaymentAllocation.Create(
                        companyId: receiving.CompanyId,
                        receivingPaymentId: receiving.Id,
                        obligationPaymentId: obligation.Id,
                        allocatedAmount: allocation.Amount,
                        allocationDate: allocationCmd.AllocationDate,
                        createdAt: DateTimeOffset.UtcNow,
                        createdBy: _userId
                    );
                    _dbContext.Set<PaymentAllocation>().Add(pa);

                    // Update obligation paid amount
                    obligation.UpdateAllocationSync(allocation.Amount, DueDateStatus.Paid, DateTimeOffset.UtcNow, _userId);
                }
            }

            if (typeof(TResponse) == typeof(MediatR.Unit))
            {
                return (TResponse)(object)MediatR.Unit.Value;
            }
            return default!;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is RecordPaymentAllocationCommand allocationCmd)
            {
                return Send<MediatR.Unit>(allocationCmd, cancellationToken);
            }
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    [Fact]
    public async Task CallbackProcess_FailureAfterReceiptGeneration_RollsBackEverythingCleanly()
    {
        // Arrange
        var context = _fixture.Context;
        var userCtx = new FakeCurrentUserContext(_userId);

        // 1. Seed pending transaction record
        var tx = EfawateercomTransaction.Create(_companyId, _rentPaymentId, "TX-ROLLBACK-99", DateTimeOffset.UtcNow.AddMinutes(-5), 500.00m, "JOD");
        context.EfawateercomTransactions.Add(tx);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var txRepo = new PropertyOS.Infrastructure.Financials.Repositories.EfawateercomTransactionRepository(context);
        var rentRepo = new PropertyOS.Infrastructure.Financials.Repositories.RentPaymentRepository(context);
        var seqRepo = new PropertyOS.Infrastructure.Financials.Repositories.CompanyReceiptSequenceRepository(context);
        var mediator = new FakeMediator(context, userCtx.UserId!.Value);

        var handler = new ReceiveEfawateercomCallbackCommandHandler(txRepo, rentRepo, seqRepo, userCtx, mediator);

        var command = new ReceiveEfawateercomCallbackCommand(
            ExternalTransactionId: "TX-ROLLBACK-99",
            Status: EfawateercomStatus.Success,
            ResponseTime: DateTimeOffset.UtcNow,
            ResponseCode: "000",
            ResponseMessage: "SUCCESS",
            RawResponse: "{\"status\":\"OK\"}"
        );

        // Act
        // Run inside an explicit database transaction block
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                // Execute handler
                await handler.Handle(command, CancellationToken.None);

                // Save changes inside transaction block
                await context.SaveChangesAsync();

                // Intentionally throw an exception to force a transaction rollback
                throw new Exception("Simulated pipeline failure after receipt generation");
            }
            catch (Exception ex) when (ex.Message == "Simulated pipeline failure after receipt generation")
            {
                await transaction.RollbackAsync();
            }
        }

        // Assert
        // Clear change tracker to ensure we query fresh state from Postgres
        context.ChangeTracker.Clear();

        // 1. Transaction record should be rolled back to Pending state in DB
        var dbTx = await context.EfawateercomTransactions.FindAsync(tx.Id);
        Assert.NotNull(dbTx);
        Assert.Equal(EfawateercomStatus.Pending, dbTx.TransactionStatus);

        // 2. No RentPaymentReceipt should exist in the database
        var dbReceiptCount = await context.Set<RentPaymentReceipt>().CountAsync(r => r.RentPaymentId == _rentPaymentId);
        Assert.Equal(0, dbReceiptCount);

        // 3. Company Receipt Sequence counter should be rolled back to 0
        var dbSequence = await context.CompanyReceiptSequences.FirstOrDefaultAsync(s => s.CompanyId == _companyId);
        Assert.NotNull(dbSequence);
        Assert.Equal(0, dbSequence.CurrentNumber);

        // 4. No UnallocatedReceipt rent payment should exist
        var dbUnallocatedCount = await context.RentPayments.CountAsync(p => p.CompanyId == _companyId && p.PaymentPurpose == PaymentPurpose.UnallocatedReceipt);
        Assert.Equal(0, dbUnallocatedCount);
    }
}
