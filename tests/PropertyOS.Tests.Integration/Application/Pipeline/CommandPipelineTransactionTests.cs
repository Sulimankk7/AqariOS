using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Application;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Commands.CreateLeaseContract;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Infrastructure;
using PropertyOS.Infrastructure.Leasing.Repositories;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Behaviors;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Application.Pipeline;

[Collection("Postgres collection")]
public class CommandPipelineTransactionTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private ServiceProvider _serviceProvider = null!;
    private Guid _companyId;
    private Guid _userId;

    public CommandPipelineTransactionTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
        _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    private void SetupDI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fixture.RawConnectionString);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.BuildingType>("building_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.Governorate>("governorate_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.FloorType>("floor_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>("ownership_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>("occupancy_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingType>("parking_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>("parking_assignment_status_enum");

        // Module 5
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");

        // Module 6
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");

        // Module 7
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");

        var dataSource = dataSourceBuilder.Build();
        
        services.AddScoped<AuditTransactionState>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditTransactionInterceptor>();
        services.AddScoped<TenantSessionInterceptor>();
        services.AddDbContext<PropertyOsDbContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource, o =>
            {
                o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
                o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
                o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
                o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");
                o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
                o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
                o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.BuildingType>("building_type_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.Governorate>("governorate_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.FloorType>("floor_type_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>("ownership_status_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>("occupancy_status_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingType>("parking_type_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>("parking_assignment_status_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");
            });
            options.AddInterceptors(
                sp.GetRequiredService<TenantSessionInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditTransactionInterceptor>()
            );
        });

        services.AddScoped<PropertyOS.Application.Leasing.ILeaseContractRepository, LeaseContractRepository>();
        services.AddScoped<PropertyOS.Application.Leasing.ILeasingReferenceRepository, LeasingReferenceRepository>();
        
        var mockTenantCtx = new FakeTenantContext { CompanyId = _companyId };
        services.AddSingleton<ITenantContext>(mockTenantCtx);
        
        var mockUserCtx = new FakeCurrentUserContext { UserId = _userId };
        services.AddSingleton<ICurrentUserContext>(mockUserCtx);

        var mockAuditCtx = new FakeAuditRequestContext { RequestId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Source = PropertyOS.Domain.Audit.Enums.AuditSource.Api };
        services.AddSingleton<IAuditRequestContext>(mockAuditCtx);
        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        
        _serviceProvider = services.BuildServiceProvider();
    }

    private class FakeTenantContext : ITenantContext { 
        public Guid? CompanyId { get; set; } 
        public bool IsPlatformAdmin { get; set; } = false;
    }
    private class FakeCurrentUserContext : ICurrentUserContext { public Guid? UserId { get; set; } }
    private class FakeAuditRequestContext : IAuditRequestContext { public Guid? RequestId { get; set; } public Guid? CorrelationId { get; set; } public PropertyOS.Domain.Audit.Enums.AuditSource Source { get; set; } }

    private sealed record LeasingSeed(Guid CompanyId, Guid BuildingId, Guid ApartmentId, Guid TenantId, Guid UserId);

    private async Task<LeasingSeed> SeedPrerequisites()
    {
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = _userId;

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (id, full_name, email) VALUES (@uId, 'Test User', 'test@example.com');
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) VALUES (@cId, 'Test', 'Test', '+962791234567', 'individual_owner', now(), now());
            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at) VALUES (@bId, @cId, 'B', 'residential', 1, now(), now());
            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at) VALUES (@fId, @cId, @bId, 1, 'Floor 1', 'regular', now(), now());
            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at) 
                VALUES (@aId, @fId, @bId, @cId, '101', 'vacant', 1, 1, 100, 100, now(), now());
            INSERT INTO tenants (id, company_id, name, national_id, phone, created_at, updated_at) 
                VALUES (@tId, @cId, 'T 1', '1234567890', '+962791234567', now(), now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("uId", userId));
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("fId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("aId", apartmentId));
        cmd.Parameters.Add(new NpgsqlParameter("tId", tenantId));
        await cmd.ExecuteNonQueryAsync();

        return new LeasingSeed(_companyId, buildingId, apartmentId, tenantId, userId);
    }

    [Fact]
    public async Task InvalidCommand_ShortCircuits_ThrowsValidationException()
    {
        var seed = await SeedPrerequisites();

        SetupDI();
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateLeaseContractCommand(seed.ApartmentId, seed.TenantId, "LC", new DateTime(2025,1,1), new DateTime(2024,1,1), 100, 100, PaymentFrequency.Monthly, 1);
        
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => mediator.Send(command));
        
        var count = await _fixture.Context.LeaseContracts.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ValidCommand_Persists_WithAudit()
    {
        var seed = await SeedPrerequisites();
        
        SetupDI();
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateLeaseContractCommand(seed.ApartmentId, seed.TenantId, "LC-SUCCESS", new DateTime(2025,1,1), new DateTime(2026,1,1), 100, 100, PaymentFrequency.Monthly, 1);
        await mediator.Send(command);

        var contract = await _fixture.Context.LeaseContracts.FirstOrDefaultAsync(c => c.ContractNumber == "LC-SUCCESS");
        Assert.NotNull(contract);
        Assert.Equal(seed.CompanyId, contract.CompanyId);
        Assert.Equal(seed.BuildingId, contract.BuildingId);
        Assert.Equal(seed.ApartmentId, contract.ApartmentId);
        Assert.Equal(seed.TenantId, contract.TenantId);
        Assert.Equal(ContractStatus.Draft, contract.Status);
        
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM audit_logs WHERE entity_id = @entityId AND action = 'create'::audit_action_enum";
        cmd.Parameters.Add(new NpgsqlParameter("entityId", contract.Id));
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task HandlerFailure_RollsBack_NoAudit()
    {
        var seed = await SeedPrerequisites();
        
        SetupDI();
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var nonExistentTenantId = Guid.NewGuid();
        var command = new CreateLeaseContractCommand(seed.ApartmentId, nonExistentTenantId, "LC-FAIL", new DateTime(2025,1,1), new DateTime(2026,1,1), 100, 100, PaymentFrequency.Monthly, 1);
        
        await Assert.ThrowsAsync<PropertyOS.Application.Common.Exceptions.NotFoundException>(() => mediator.Send(command));

        var count = await _fixture.Context.LeaseContracts.CountAsync();
        Assert.Equal(0, count);
        
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM audit_logs";
        var auditCount = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(0, auditCount);
    }

    [Fact]
    public async Task DatabaseFailure_RollsBack_NoAudit()
    {
        var seed = await SeedPrerequisites();
        
        SetupDI();
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var contract1 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-DB-FAIL", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, seed.UserId, null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Expired);
        _fixture.Context.LeaseContracts.Add(contract1);
        await _fixture.Context.SaveChangesAsync();

        var contract2 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-DB-2", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, seed.UserId, null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Draft);
        _fixture.Context.LeaseContracts.Add(contract2);
        await _fixture.Context.SaveChangesAsync(); 

        var docFileId = Guid.NewGuid();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO file_storage (id, company_id, original_filename, mime_type, size_bytes, storage_key, created_at, updated_at) VALUES ({0}, {1}, 'doc.pdf', 'application/pdf', 1024, {2}, now(), now())",
            docFileId, seed.CompanyId, "key-pipe-" + docFileId);

        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO contract_documents (id, company_id, lease_contract_id, file_id, document_type, created_at, updated_at) VALUES (gen_random_uuid(), {0}, {1}, {2}, 'signed_contract', now(), now())",
            seed.CompanyId, contract2.Id, docFileId); 

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM audit_logs";
        var auditCountBefore = (long)(await cmd.ExecuteScalarAsync() ?? 0L);

        // Inject non-existent UserId into FakeCurrentUserContext to trigger foreign key violation in database on save
        var userCtx = (FakeCurrentUserContext)scope.ServiceProvider.GetRequiredService<ICurrentUserContext>();
        userCtx.UserId = Guid.NewGuid();

        var activateCommand = new PropertyOS.Application.Leasing.Commands.ActivateLeaseContract.ActivateLeaseContractCommand(contract2.Id);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => mediator.Send(activateCommand));

        var pgEx = ex.InnerException as PostgresException;
        Assert.NotNull(pgEx);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, pgEx.SqlState);
        Assert.Contains("fk_contract_status_history_users_changed_", pgEx.ConstraintName);

        _fixture.Context.ChangeTracker.Clear();

        var freshContract2 = await _fixture.Context.LeaseContracts.SingleAsync(c => c.Id == contract2.Id);
        Assert.Equal(ContractStatus.Draft, freshContract2.Status);

        var freshContract1 = await _fixture.Context.LeaseContracts.SingleAsync(c => c.Id == contract1.Id);
        Assert.Equal(ContractStatus.Expired, freshContract1.Status);

        var historyRows = await _fixture.Context.ContractStatusHistory.Where(h => h.LeaseContractId == contract2.Id).ToListAsync();
        Assert.Empty(historyRows); 

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT count(*) FROM audit_logs";
        var auditCountAfter = (long)(await cmd2.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(auditCountBefore, auditCountAfter);
    }
}
