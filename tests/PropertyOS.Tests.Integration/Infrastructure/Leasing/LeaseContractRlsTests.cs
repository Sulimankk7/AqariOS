using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Leasing;

[Collection("Postgres collection")]
public class LeaseContractRlsTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public LeaseContractRlsTests(PostgresTestFixture fixture)
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
                o.MapEnum<ContractDocumentType>("contract_document_type_enum");
                o.MapEnum<TerminationType>("termination_type_enum");
                o.MapEnum<ContractStatus>("contract_status_enum");
                o.MapEnum<LegalRegime>("legal_regime_enum");
                o.MapEnum<TenantType>("tenant_type_enum");
                o.MapEnum<PaymentFrequency>("payment_frequency_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    private sealed record TestData(Guid CompanyId, Guid BuildingId, Guid ApartmentId, Guid TenantId, Guid ContractId);

    private async Task<(TestData A, TestData B)> SeedAndSetupDataAsync()
    {
        Guid companyA_Id = Guid.Empty;
        Guid companyB_Id = Guid.Empty;
        var dataA = new TestData(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
        var dataB = new TestData(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);

        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            await adminCtx.Database.BeginTransactionAsync();

            var compA = Company.Create("Legal A", "Display A", "+962790000001", CompanyType.PropertyManagementCompany, "JO", DateTimeOffset.UtcNow, null, "CR1", "TAX1", "a@test.com");
            var compB = Company.Create("Legal B", "Display B", "+962790000002", CompanyType.PropertyManagementCompany, "JO", DateTimeOffset.UtcNow, null, "CR2", "TAX2", "b@test.com");
            adminCtx.Set<Company>().Add(compA);
            adminCtx.Set<Company>().Add(compB);
            await adminCtx.SaveChangesAsync();

            companyA_Id = compA.Id;
            companyB_Id = compB.Id;
            await adminCtx.Database.CommitTransactionAsync();
        }

        // Use raw SQL to insert infrastructure that bypasses ORM mapping for brevity or directly inject them to test RLS
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        
        var buildA = Guid.NewGuid(); var buildB = Guid.NewGuid();
        var aptA = Guid.NewGuid(); var aptB = Guid.NewGuid();
        var tenA = Guid.NewGuid(); var tenB = Guid.NewGuid();
        var contractAId = Guid.NewGuid(); var contractBId = Guid.NewGuid();

        cmd.CommandText = @"
            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at) VALUES (@buildA, @cA, 'BA', 'residential', 1, now(), now()), (@buildB, @cB, 'BB', 'residential', 1, now(), now());
            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at) VALUES (gen_random_uuid(), @cA, @buildA, 1, 'Floor 1', 'regular', now(), now()), (gen_random_uuid(), @cB, @buildB, 1, 'Floor 1', 'regular', now(), now());
            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at) 
                VALUES (@aptA, (SELECT id FROM floors WHERE building_id = @buildA LIMIT 1), @buildA, @cA, '101A', 'vacant', 1, 1, 100, 100, now(), now()),
                       (@aptB, (SELECT id FROM floors WHERE building_id = @buildB LIMIT 1), @buildB, @cB, '101B', 'vacant', 1, 1, 100, 100, now(), now());
            INSERT INTO tenants (id, company_id, name, national_id, phone, created_at, updated_at) 
                VALUES (@tenA, @cA, 'TA 1', '1234567890', '+962791234567', now(), now()), (@tenB, @cB, 'TB 1', '1234567890', '+962791234568', now(), now());
            
            INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, status, legal_regime, tenant_type, security_deposit_amount, created_at, updated_at)
                VALUES (@contA, @cA, @buildA, @aptA, @tenA, 'LC-A', '2025-01-01', '2026-01-01', 100, 'monthly', 1, 'draft', 'standard', 'personal', 100, now(), now()),
                       (@contB, @cB, @buildB, @aptB, @tenB, 'LC-B', '2025-01-01', '2026-01-01', 100, 'monthly', 1, 'draft', 'standard', 'personal', 100, now(), now());
            
            INSERT INTO contract_terminations (id, company_id, lease_contract_id, termination_date, termination_type, reason, created_at, updated_at)
                VALUES (gen_random_uuid(), @cA, @contA, '2025-07-01', 'mutual_agreement', 'N/A', now(), now()),
                       (gen_random_uuid(), @cB, @contB, '2025-07-01', 'mutual_agreement', 'N/A', now(), now());

            INSERT INTO contract_status_history (id, company_id, lease_contract_id, previous_status, new_status, reason, changed_at)
                VALUES (gen_random_uuid(), @cA, @contA, NULL, 'draft', 'init', now()),
                       (gen_random_uuid(), @cB, @contB, NULL, 'draft', 'init', now());

            INSERT INTO file_storage (id, company_id, original_filename, mime_type, size_bytes, storage_key, created_at, updated_at)
                VALUES (@fileA, @cA, 'leaseA.pdf', 'application/pdf', 1024, 'key-lease-rls-a', now(), now()),
                       (@fileB, @cB, 'leaseB.pdf', 'application/pdf', 1024, 'key-lease-rls-b', now(), now());

            INSERT INTO contract_documents (id, company_id, lease_contract_id, file_id, document_type, created_at, updated_at)
                VALUES (gen_random_uuid(), @cA, @contA, @fileA, 'signed_contract', now(), now()),
                       (gen_random_uuid(), @cB, @contB, @fileB, 'signed_contract', now(), now());
        ";
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        cmd.Parameters.Add(new NpgsqlParameter("fileA", fileA));
        cmd.Parameters.Add(new NpgsqlParameter("fileB", fileB));
        cmd.Parameters.Add(new NpgsqlParameter("cA", companyA_Id));
        cmd.Parameters.Add(new NpgsqlParameter("cB", companyB_Id));
        cmd.Parameters.Add(new NpgsqlParameter("buildA", buildA));
        cmd.Parameters.Add(new NpgsqlParameter("buildB", buildB));
        cmd.Parameters.Add(new NpgsqlParameter("aptA", aptA));
        cmd.Parameters.Add(new NpgsqlParameter("aptB", aptB));
        cmd.Parameters.Add(new NpgsqlParameter("tenA", tenA));
        cmd.Parameters.Add(new NpgsqlParameter("tenB", tenB));
        cmd.Parameters.Add(new NpgsqlParameter("contA", contractAId));
        cmd.Parameters.Add(new NpgsqlParameter("contB", contractBId));

        await cmd.ExecuteNonQueryAsync();

        dataA = new TestData(companyA_Id, buildA, aptA, tenA, contractAId);
        dataB = new TestData(companyB_Id, buildB, aptB, tenB, contractBId);

        return (dataA, dataB);
    }

    [Fact]
    public async Task LeaseContracts_RLS_Enforced()
    {
        var (dataA, dataB) = await SeedAndSetupDataAsync();

        await using var ctxA = CreateContext(dataA.CompanyId);
        await ctxA.Database.BeginTransactionAsync();

        var contracts = await ctxA.LeaseContracts.IgnoreQueryFilters().ToListAsync();
        Assert.Single(contracts);
        Assert.Equal(dataA.ContractId, contracts[0].Id);
        Assert.Equal(dataA.CompanyId, contracts[0].CompanyId);
        Assert.DoesNotContain(contracts, c => c.Id == dataB.ContractId);

        await ctxA.Database.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ContractTerminations_RLS_Enforced()
    {
        var (dataA, dataB) = await SeedAndSetupDataAsync();

        await using var ctxA = CreateContext(dataA.CompanyId);
        await ctxA.Database.BeginTransactionAsync();

        var terms = await ctxA.ContractTerminations.IgnoreQueryFilters().ToListAsync();
        Assert.Single(terms);
        Assert.Equal(dataA.CompanyId, terms[0].CompanyId);
        Assert.Equal(dataA.ContractId, terms[0].LeaseContractId);
        Assert.DoesNotContain(terms, t => t.CompanyId == dataB.CompanyId);

        await ctxA.Database.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ContractStatusHistory_RLS_Enforced()
    {
        var (dataA, dataB) = await SeedAndSetupDataAsync();

        await using var ctxA = CreateContext(dataA.CompanyId);
        await ctxA.Database.BeginTransactionAsync();

        var hists = await ctxA.ContractStatusHistory.IgnoreQueryFilters().ToListAsync();
        Assert.Single(hists);
        Assert.Equal(dataA.CompanyId, hists[0].CompanyId);
        Assert.Equal(dataA.ContractId, hists[0].LeaseContractId);
        Assert.DoesNotContain(hists, h => h.CompanyId == dataB.CompanyId);

        await ctxA.Database.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ContractDocuments_RLS_Enforced()
    {
        var (dataA, dataB) = await SeedAndSetupDataAsync();

        await using var ctxA = CreateContext(dataA.CompanyId);
        await ctxA.Database.BeginTransactionAsync();

        var docs = await ctxA.Set<ContractDocument>().IgnoreQueryFilters().ToListAsync();
        Assert.Single(docs);
        Assert.Equal(dataA.CompanyId, docs[0].CompanyId);
        Assert.Equal(dataA.ContractId, docs[0].LeaseContractId);
        Assert.DoesNotContain(docs, d => d.CompanyId == dataB.CompanyId);

        await ctxA.Database.RollbackTransactionAsync();
    }
}
