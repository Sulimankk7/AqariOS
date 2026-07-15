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
        var dataSource = dataSourceBuilder.Build();
        
        services.AddScoped<AuditTransactionState>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditTransactionInterceptor>();
        services.AddScoped<TenantSessionInterceptor>();
        services.AddDbContext<PropertyOsDbContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource);
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

    private sealed record LeasingSeed(Guid CompanyId, Guid BuildingId, Guid ApartmentId, Guid TenantId);

    private async Task<LeasingSeed> SeedPrerequisites()
    {
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO companies (id, name, type, created_at) VALUES (@cId, 'Test', 'Owner', now());
            INSERT INTO buildings (id, company_id, name, created_at) VALUES (@bId, @cId, 'B', now());
            INSERT INTO floors (id, building_id, number, created_at) VALUES (@fId, @bId, 1, now());
            INSERT INTO apartments (id, floor_id, building_id, company_id, number, type, bedrooms, bathrooms, base_rent_amount, size_sqm, created_at) 
                VALUES (@aId, @fId, @bId, @cId, '101', 'Residential', 1, 1, 100, 100, now());
            INSERT INTO tenants (id, company_id, type, first_name, last_name, phone_number, created_at) 
                VALUES (@tId, @cId, 'Personal', 'T', '1', '123', now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("fId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("aId", apartmentId));
        cmd.Parameters.Add(new NpgsqlParameter("tId", tenantId));
        await cmd.ExecuteNonQueryAsync();

        return new LeasingSeed(_companyId, buildingId, apartmentId, tenantId);
    }

    [Fact]
    public async Task InvalidCommand_ShortCircuits_ThrowsValidationException()
    {
        SetupDI();
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC", new DateTime(2025,1,1), new DateTime(2024,1,1), 100, 100, PaymentFrequency.Monthly, 1);
        
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
        cmd.CommandText = "SELECT count(*) FROM audit_logs WHERE entity_id = @entityId AND action = 'Create'";
        cmd.Parameters.Add(new NpgsqlParameter("entityId", contract.Id.ToString()));
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
        
        await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(() => mediator.Send(command));

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

        var contract1 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-DB-FAIL", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        _fixture.Context.LeaseContracts.Add(contract1);
        await _fixture.Context.SaveChangesAsync();

        var contract2 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-DB-2", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Draft);
        _fixture.Context.LeaseContracts.Add(contract2);
        await _fixture.Context.SaveChangesAsync(); 

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM audit_logs";
        var auditCountBefore = (long)(await cmd.ExecuteScalarAsync() ?? 0L);

        var activateCommand = new PropertyOS.Application.Leasing.Commands.ActivateLeaseContract.ActivateLeaseContractCommand(contract2.Id);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => mediator.Send(activateCommand));

        var pgEx = ex.InnerException as PostgresException;
        Assert.NotNull(pgEx);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pgEx.SqlState);
        Assert.Contains("uq_lease_contracts_one_active_per_apartment", pgEx.ConstraintName);

        var freshOptions = new DbContextOptionsBuilder<PropertyOsDbContext>().UseNpgsql(conn).Options;
        await using var freshContext = new PropertyOsDbContext(freshOptions);

        var freshContract2 = await freshContext.LeaseContracts.SingleAsync(c => c.Id == contract2.Id);
        Assert.Equal(ContractStatus.Draft, freshContract2.Status);

        var freshContract1 = await freshContext.LeaseContracts.SingleAsync(c => c.Id == contract1.Id);
        Assert.Equal(ContractStatus.Active, freshContract1.Status);

        var historyRows = await freshContext.ContractStatusHistory.Where(h => h.LeaseContractId == contract2.Id).ToListAsync();
        Assert.Empty(historyRows); // ActivateLeaseContractCommandHandler does not stage history currently, or if it did, it rolled back.

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT count(*) FROM audit_logs";
        var auditCountAfter = (long)(await cmd2.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(auditCountBefore, auditCountAfter);
    }
}
