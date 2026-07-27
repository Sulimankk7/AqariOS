using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Leasing.Jobs;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Leasing;

[Collection("Postgres collection")]
public class LeaseExpirationIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public LeaseExpirationIntegrationTests(PostgresTestFixture fixture)
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

    // -------------------------------------------------------------------------
    // Context factory helpers (used for direct-context setup and verification)
    // -------------------------------------------------------------------------

    private sealed class StaticTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin { get; set; }
    }

    private sealed class StaticCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; }
    }

    private class StaticAuditRequestContext : IAuditRequestContext
    {
        public Guid? RequestId => null;
        public Guid? CorrelationId => null;
        public PropertyOS.Domain.Audit.Enums.AuditSource Source => PropertyOS.Domain.Audit.Enums.AuditSource.SystemJob;
    }

    /// <summary>
    /// Creates a PropertyOsDbContext wired to the correct connection + interceptors
    /// for the given tenant context. Used for test setup and verification queries.
    /// </summary>
    private PropertyOsDbContext CreateContext(Guid? companyId, Guid? userId = null, bool isPlatformAdmin = false, bool forceAdminConnection = false)
    {
        var tenantContext = new StaticTenantContext { CompanyId = companyId, IsPlatformAdmin = isPlatformAdmin };
        var userContext = new StaticCurrentUserContext { UserId = userId };

        var connection = forceAdminConnection
            ? _fixture.Context.Database.GetDbConnection()
            : _sharedAppUserConnection!;

        var auditState = new AuditTransactionState();
        var auditTxInterceptor = new AuditTransactionInterceptor(auditState);
        var auditSaveChangesInterceptor = new AuditSaveChangesInterceptor(tenantContext, userContext, auditState, new StaticAuditRequestContext());

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(connection, npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<CompanyType>("company_type_enum");
                npgsqlOptions.MapEnum<BuildingType>("building_type_enum");
                npgsqlOptions.MapEnum<FloorType>("floor_type_enum");
                npgsqlOptions.MapEnum<OwnershipStatus>("ownership_status_enum");
                npgsqlOptions.MapEnum<OccupancyStatus>("occupancy_status_enum");
                npgsqlOptions.MapEnum<ContractStatus>("contract_status_enum");
                npgsqlOptions.MapEnum<PaymentFrequency>("payment_frequency_enum");
                npgsqlOptions.MapEnum<TerminationType>("termination_type_enum");
                npgsqlOptions.MapEnum<ContractDocumentType>("contract_document_type_enum");
                npgsqlOptions.MapEnum<LegalRegime>("legal_regime_enum");
                npgsqlOptions.MapEnum<TenantType>("tenant_type_enum");
                npgsqlOptions.MapEnum<AuditAction>("audit_action_enum");
                npgsqlOptions.MapEnum<AuditSeverity>("audit_severity_enum");
                npgsqlOptions.MapEnum<AuditSource>("audit_source_enum");
                npgsqlOptions.MapEnum<MfaType>("mfa_type_enum");
                npgsqlOptions.MapEnum<LoginStatus>("login_status_enum");
                npgsqlOptions.MapEnum<MembershipStatus>("membership_status_enum");
                npgsqlOptions.MapEnum<OtpPurpose>("otp_purpose_enum");
                npgsqlOptions.MapEnum<RevokeReason>("revoke_reason_enum");
            })
            .AddInterceptors(
                new TenantSessionInterceptor(tenantContext, userContext),
                auditSaveChangesInterceptor,
                auditTxInterceptor)
            .Options;

        return new PropertyOsDbContext(options);
    }

    /// <summary>
    /// Seeds a complete property hierarchy (Company → Building → Floor → Apartment → Tenant)
    /// using platform-admin scope for the company and tenant scope for properties.
    /// </summary>
    private async Task<(Guid companyId, Guid buildingId, Guid apartmentId, Guid tenantId, Guid userId)>
        SeedPropertyHierarchyAsync(Guid? overrideCompanyId = null)
    {
        var companyId = overrideCompanyId ?? Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed Company and User via platform admin
        using (var adminDb = CreateContext(null, isPlatformAdmin: true, forceAdminConnection: true))
        {
            await using var tx = await adminDb.Database.BeginTransactionAsync();
            
            var user = new PropertyOS.Domain.Identity.Entities.User
            {
                Id = userId,
                FullName = "Test User",
                Email = $"test-{userId}@example.com",
                PasswordHash = "hash",
                PasswordAlgorithm = "argon2id",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };
            adminDb.Set<PropertyOS.Domain.Identity.Entities.User>().Add(user);

            if (!overrideCompanyId.HasValue)
            {
                var company = Company.Create(
                    legalName: "Company " + companyId.ToString()[..8],
                    displayName: "Display " + companyId.ToString()[..8],
                    primaryPhone: "+962790000000",
                    companyType: CompanyType.IndividualOwner,
                    countryCode: "JO",
                    createdAt: DateTimeOffset.UtcNow,
                    createdBy: null);
                typeof(Company).GetProperty(nameof(Company.Id))!.SetValue(company, companyId);
                adminDb.Companies.Add(company);
            }
            await adminDb.SaveChangesAsync();
            await tx.CommitAsync();
            userId = user.Id;
        }

        // Seed Building, Floor, Apartment, Tenant via tenant context
        using (var db = CreateContext(companyId, userId))
        {
            await using var tx = await db.Database.BeginTransactionAsync();

            var building = Building.Create(
                companyId: companyId,
                name: "Bld " + Guid.NewGuid().ToString()[..4],
                totalFloors: 5,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: null,
                internalCode: "Code-" + Guid.NewGuid().ToString()[..4]);
            db.Buildings.Add(building);
            await db.SaveChangesAsync(); // Boundary 1: Database generates building.Id

            var floor = Floor.Create(
                companyId: companyId,
                buildingId: building.Id,
                floorNumber: 1,
                floorLabel: "Floor 1",
                createdAt: DateTimeOffset.UtcNow,
                createdBy: null);
            db.Floors.Add(floor);
            await db.SaveChangesAsync(); // Boundary 2: Database generates floor.Id

            var apartment = Apartment.Create(
                companyId: companyId,
                buildingId: building.Id,
                floorId: floor.Id,
                unitNumber: "101",
                areaSqm: 100,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: null,
                bedrooms: 2,
                bathrooms: 1,
                baseRentAmount: 500);
            db.Apartments.Add(apartment);

            var tenant = Tenant.Create(
                companyId: companyId,
                name: "John Doe",
                nationalId: "NAT-" + Guid.NewGuid().ToString()[..8],
                phone: "+962790000000",
                createdAt: DateTimeOffset.UtcNow,
                createdBy: null);
            db.Tenants.Add(tenant);

            await db.SaveChangesAsync(); // Boundary 3: Database generates apartment.Id and tenant.Id
            await tx.CommitAsync();
            return (companyId, building.Id, apartment.Id, tenant.Id, userId);
        }
    }

    /// <summary>
    /// Seeds an Active lease contract that is eligible for expiration
    /// (EndDate = yesterday relative to UTC today).
    /// </summary>
    private async Task<Guid> SeedEligibleActiveLeaseAsync(Guid companyId, Guid buildingId, Guid apartmentId, Guid tenantId, Guid userId, string contractNumber)
    {
        using var db = CreateContext(companyId, userId);
        await using var tx = await db.Database.BeginTransactionAsync();

        var contract = LeaseContract.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: apartmentId,
            tenantId: tenantId,
            contractNumber: contractNumber,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),  // already ended
            monthlyRentAmount: 500,
            paymentFrequency: PaymentFrequency.Monthly,
            paymentDueDay: 1,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userId,
            priorContractId: null,
            legalRegime: LegalRegime.Standard,
            tenantType: TenantType.Personal,
            securityDepositAmount: 500,
            status: ContractStatus.Active);

        db.LeaseContracts.Add(contract);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return contract.Id;
    }

    // =========================================================================
    // Test 1: Direct-context RLS — tenant isolation (existing, fixed compilation)
    // =========================================================================

    [Fact]
    public async Task Expiration_TenantIsolation_OnlyQueriesCompanyContracts()
    {
        // Arrange: two companies each with one Active lease
        var (companyA, bldA, aptA, tenantA, userA) = await SeedPropertyHierarchyAsync();
        var (companyB, bldB, aptB, tenantB, userB) = await SeedPropertyHierarchyAsync();

        Guid contractAId, contractBId;

        using (var dbA = CreateContext(companyA, userA))
        {
            await using var txA = await dbA.Database.BeginTransactionAsync();
            var contractA = LeaseContract.Create(
                companyId: companyA,
                buildingId: bldA,
                apartmentId: aptA,
                tenantId: tenantA,
                contractNumber: "LC-A",
                startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                monthlyRentAmount: 100,
                paymentFrequency: PaymentFrequency.Monthly,
                paymentDueDay: 1,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: userA,
                priorContractId: null,
                legalRegime: LegalRegime.Standard,
                tenantType: TenantType.Personal,
                securityDepositAmount: 100,
                status: ContractStatus.Active);
            dbA.LeaseContracts.Add(contractA);
            await dbA.SaveChangesAsync();
            await txA.CommitAsync();
            contractAId = contractA.Id;
        }

        using (var dbB = CreateContext(companyB, userB))
        {
            await using var txB = await dbB.Database.BeginTransactionAsync();
            var contractB = LeaseContract.Create(
                companyId: companyB,
                buildingId: bldB,
                apartmentId: aptB,
                tenantId: tenantB,
                contractNumber: "LC-B",
                startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                monthlyRentAmount: 100,
                paymentFrequency: PaymentFrequency.Monthly,
                paymentDueDay: 1,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: userB,
                priorContractId: null,
                legalRegime: LegalRegime.Standard,
                tenantType: TenantType.Personal,
                securityDepositAmount: 100,
                status: ContractStatus.Active);
            dbB.LeaseContracts.Add(contractB);
            await dbB.SaveChangesAsync();
            await txB.CommitAsync();
            contractBId = contractB.Id;
        }

        // Assert: Company A context only sees Company A contracts
        using (var dbA = CreateContext(companyA, userA))
        {
            await using var txA = await dbA.Database.BeginTransactionAsync();
            var contractsA = await dbA.LeaseContracts.ToListAsync();
            contractsA.Should().ContainSingle(c => c.Id == contractAId);
            contractsA.Should().NotContain(c => c.Id == contractBId);
        }

        // Assert: Company B context only sees Company B contracts
        using (var dbB = CreateContext(companyB, userB))
        {
            await using var txB = await dbB.Database.BeginTransactionAsync();
            var contractsB = await dbB.LeaseContracts.ToListAsync();
            contractsB.Should().ContainSingle(c => c.Id == contractBId);
            contractsB.Should().NotContain(c => c.Id == contractAId);
        }
    }

    // =========================================================================
    // Test 2: Occupancy trigger test (existing, no changes needed)
    // =========================================================================

    [Fact]
    public async Task Expiration_PostgresTrigger_ReconcilesApartmentOccupancyStatusToVacant()
    {
        // 1. Arrange & Seed
        var (companyId, bldId, apartmentId, tenantId, userId) = await SeedPropertyHierarchyAsync();
        Guid contractId;

        // 1. Seed Active lease → apartment trigger sets Occupied
        using (var db = CreateContext(companyId, userId))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var contract = LeaseContract.Create(
                companyId: companyId,
                buildingId: bldId,
                apartmentId: apartmentId,
                tenantId: tenantId,
                contractNumber: "LC-OCC-1",
                startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                monthlyRentAmount: 100,
                paymentFrequency: PaymentFrequency.Monthly,
                paymentDueDay: 1,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: userId,
                priorContractId: null,
                legalRegime: LegalRegime.Standard,
                tenantType: TenantType.Personal,
                securityDepositAmount: 100,
                status: ContractStatus.Active);
            db.LeaseContracts.Add(contract);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            contractId = contract.Id;
        }

        // Verify Occupied — must open explicit transaction so TenantSessionInterceptor
        // fires SET LOCAL app.current_company_id and RLS permits the query.
        using (var db = CreateContext(companyId, userId))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var apt = await db.Apartments.FirstAsync(a => a.Id == apartmentId);
            apt.OccupancyStatus.Should().Be(OccupancyStatus.Occupied);
            await tx.RollbackAsync();
        }

        // 2. Expire lease → trigger sets Vacant
        using (var db = CreateContext(companyId, userId))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var contract = await db.LeaseContracts.FirstAsync(c => c.Id == contractId);
            contract.Expire(DateTimeOffset.UtcNow, userId);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // Verify Vacant — explicit transaction required for the same reason.
        using (var db = CreateContext(companyId, userId))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var apt = await db.Apartments.FirstAsync(a => a.Id == apartmentId);
            apt.OccupancyStatus.Should().Be(OccupancyStatus.Vacant);
            await tx.RollbackAsync();
        }
    }

    // =========================================================================
    // Test 3: Real job path — exercises full DI stack against PostgreSQL
    //
    // This test exercises the complete execution path:
    //   ExpireLeaseContractsJob
    //   → IServiceProvider.CreateScope()
    //   → ISystemTenantContextSetter.SetPlatformAdminScope / SetCompanyScope
    //   → IApplicationDbContext.BeginTransactionAsync
    //   → TenantSessionInterceptor fires → SET LOCAL app.current_company_id
    //   → PostgreSQL RLS evaluates
    //   → ICompanyRepository.GetActiveCompanyIdsAsync / ILeaseContractRepository.GetActiveContractIdsExpiringOnOrBeforeAsync
    //   → ISender.Send(ExpireLeaseContractCommand)
    //   → TransactionBehavior → BeginTransaction → TenantSessionInterceptor again
    //   → ExpireLeaseContractCommandHandler
    //   → contract.Expire + ContractStatusHistory.Create
    //   → SaveChanges + CommitTransaction
    //   → PostgreSQL trigger sets apartment Vacant
    // =========================================================================

    [Fact]
    public async Task RealJob_ExecuteSweepAsync_ExpiresLeaseAcrossTwoCompaniesWithRlsIsolation()
    {
        // -------------------------------------------------------------------------
        // Arrange: Seed Company A with THREE distinct apartments (one Active lease
        // per apartment — required by uq_lease_contracts_one_active_per_apartment).
        // Company B gets one apartment with one Active lease.
        // -------------------------------------------------------------------------

        // Company A — first hierarchy (also establishes companyA and userA)
        var (companyA, bldA1, aptA1, tenantA1, userA) = await SeedPropertyHierarchyAsync();

        // Company A — second and third hierarchies reuse companyA's ID so they
        // land in the same tenant but in distinct buildings/apartments/tenants.
        var (_, bldA2, aptA2, tenantA2, _) = await SeedPropertyHierarchyAsync(overrideCompanyId: companyA);
        var (_, bldA3, aptA3, tenantA3, _) = await SeedPropertyHierarchyAsync(overrideCompanyId: companyA);

        // Company B — independent hierarchy
        var (companyB, bldB, aptB, tenantB, userB) = await SeedPropertyHierarchyAsync();

        // Confirm all three Company A apartments are distinct (defence-in-depth assertion)
        aptA1.Should().NotBe(aptA2);
        aptA1.Should().NotBe(aptA3);
        aptA2.Should().NotBe(aptA3);

        // Each Active lease targets a DIFFERENT apartment → satisfies the constraint
        var cA_1 = await SeedEligibleActiveLeaseAsync(companyA, bldA1, aptA1, tenantA1, userA, "LC-A1");
        var cA_2 = await SeedEligibleActiveLeaseAsync(companyA, bldA2, aptA2, tenantA2, userA, "LC-A2");
        var cA_3 = await SeedEligibleActiveLeaseAsync(companyA, bldA3, aptA3, tenantA3, userA, "LC-A3");
        var cB_1 = await SeedEligibleActiveLeaseAsync(companyB, bldB,  aptB,  tenantB,  userB, "LC-B1");

        // -------------------------------------------------------------------------
        // Build a real DI ServiceProvider wired to the test PostgreSQL container.
        // This mirrors production DI (DependencyInjection.cs) but uses the
        // test NpgsqlDataSource and test connection strings.
        // -------------------------------------------------------------------------
        var services = new ServiceCollection();

        // Register logging (NullLogger for tests)
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        // Register EF interceptors (scoped)
        services.AddScoped<TenantSessionInterceptor>();
        services.AddScoped<AuditTransactionInterceptor>();
        services.AddScoped<AuditTransactionState>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IAuditRequestContext, StaticAuditRequestContext>();

        // Register tenant context stack (mirrors DependencyInjection.cs)
        services.AddScoped<PropertyOS.Infrastructure.Identity.ClaimsPrincipalTenantContext>(
            _ => new PropertyOS.Infrastructure.Identity.ClaimsPrincipalTenantContext(
                new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        services.AddScoped<BackgroundTenantContext>(sp =>
            new BackgroundTenantContext(
                sp.GetRequiredService<PropertyOS.Infrastructure.Identity.ClaimsPrincipalTenantContext>()));
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<BackgroundTenantContext>());
        services.AddScoped<ISystemTenantContextSetter>(sp => sp.GetRequiredService<BackgroundTenantContext>());
        services.AddScoped<ICurrentUserContext, PropertyOS.Infrastructure.Identity.ClaimsPrincipalCurrentUserContext>(
            sp => new PropertyOS.Infrastructure.Identity.ClaimsPrincipalCurrentUserContext(
                new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        // Register DbContext using the test AppUser data source (real RLS-enforced connection)
        services.AddDbContext<PropertyOsDbContext>((sp, options) =>
        {
            options.UseNpgsql(_fixture.AppUserDataSource!, npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<CompanyType>("company_type_enum");
                npgsqlOptions.MapEnum<BuildingType>("building_type_enum");
                npgsqlOptions.MapEnum<FloorType>("floor_type_enum");
                npgsqlOptions.MapEnum<OwnershipStatus>("ownership_status_enum");
                npgsqlOptions.MapEnum<OccupancyStatus>("occupancy_status_enum");
                npgsqlOptions.MapEnum<ContractStatus>("contract_status_enum");
                npgsqlOptions.MapEnum<PaymentFrequency>("payment_frequency_enum");
                npgsqlOptions.MapEnum<TerminationType>("termination_type_enum");
                npgsqlOptions.MapEnum<ContractDocumentType>("contract_document_type_enum");
                npgsqlOptions.MapEnum<LegalRegime>("legal_regime_enum");
                npgsqlOptions.MapEnum<TenantType>("tenant_type_enum");
                npgsqlOptions.MapEnum<AuditAction>("audit_action_enum");
                npgsqlOptions.MapEnum<AuditSeverity>("audit_severity_enum");
                npgsqlOptions.MapEnum<AuditSource>("audit_source_enum");
                npgsqlOptions.MapEnum<MfaType>("mfa_type_enum");
                npgsqlOptions.MapEnum<LoginStatus>("login_status_enum");
                npgsqlOptions.MapEnum<MembershipStatus>("membership_status_enum");
                npgsqlOptions.MapEnum<OtpPurpose>("otp_purpose_enum");
                npgsqlOptions.MapEnum<RevokeReason>("revoke_reason_enum");
            });
            var tenantInterceptor = sp.GetRequiredService<TenantSessionInterceptor>();
            var auditInterceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
            var auditTxInterceptor = sp.GetRequiredService<AuditTransactionInterceptor>();
            options.AddInterceptors(tenantInterceptor, auditInterceptor, auditTxInterceptor);
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<PropertyOsDbContext>());

        // Register repositories (mirrors DependencyInjection.cs)
        services.AddScoped<PropertyOS.Application.Companies.ICompanyRepository,
            PropertyOS.Infrastructure.Companies.Repositories.CompanyRepository>();
        services.AddScoped<PropertyOS.Application.Leasing.ILeaseContractRepository,
            PropertyOS.Infrastructure.Leasing.Repositories.LeaseContractRepository>();

        // Register MediatR with all handlers from Application assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(PropertyOS.Application.Leasing.Commands.ExpireLeaseContract.ExpireLeaseContractCommand).Assembly);
        });
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>),
            typeof(PropertyOS.Infrastructure.Persistence.Behaviors.TransactionBehavior<,>));

        // Register IBusinessClock (singleton — will throw at startup if timezone missing)
        services.AddSingleton<IBusinessClock>(
            new PropertyOS.Infrastructure.Common.Clock.JordanBusinessClock());

        var provider = services.BuildServiceProvider();

        // -------------------------------------------------------------------------
        // Build and run the actual job
        // -------------------------------------------------------------------------
        var job = new ExpireLeaseContractsJob(
            provider,
            provider.GetRequiredService<IBusinessClock>(),
            provider.GetRequiredService<ILogger<ExpireLeaseContractsJob>>());

        // asOf = today UTC (both leases ended yesterday, so both are eligible)
        var successCount = await job.ExecuteSweepAsync(
            asOf: DateTimeOffset.UtcNow,
            batchSize: 100);

        // -------------------------------------------------------------------------
        // Assert: both leases expired
        // -------------------------------------------------------------------------
        successCount.Should().Be(4, "all leases should have been expired");

        // Verify lease A is Expired (read under Company A context)
        using (var dbA = CreateContext(companyA, userA))
        {
            await using var txA = await dbA.Database.BeginTransactionAsync();
            var contractA = await dbA.LeaseContracts.FirstAsync(c => c.Id == cA_1);
            contractA.Status.Should().Be(ContractStatus.Expired,
                "Lease A should be Expired after the job ran");

            // Verify exactly one status history entry for this expiration
            var historyA = await dbA.ContractStatusHistory
                .Where(h => h.LeaseContractId == cA_1)
                .ToListAsync();
            historyA.Should().ContainSingle(h =>
                h.NewStatus == ContractStatus.Expired &&
                h.PreviousStatus == ContractStatus.Active,
                "exactly one Active→Expired history entry should exist");
        }

        // Verify lease B is Expired (read under Company B context)
        using (var dbB = CreateContext(companyB, userB))
        {
            await using var txB = await dbB.Database.BeginTransactionAsync();
            var contractB = await dbB.LeaseContracts.FirstAsync(c => c.Id == cB_1);
            contractB.Status.Should().Be(ContractStatus.Expired,
                "Lease B should be Expired after the job ran");

            var historyB = await dbB.ContractStatusHistory
                .Where(h => h.LeaseContractId == cB_1)
                .ToListAsync();
            historyB.Should().ContainSingle(h =>
                h.NewStatus == ContractStatus.Expired &&
                h.PreviousStatus == ContractStatus.Active);
        }

        // Verify cross-tenant isolation: Company A cannot see Company B's lease
        using (var dbA = CreateContext(companyA, userA))
        {
            await using var txA = await dbA.Database.BeginTransactionAsync();
            var contractBUnderA = await dbA.LeaseContracts
                .FirstOrDefaultAsync(c => c.Id == cB_1);
            contractBUnderA.Should().BeNull(
                "Company A context must not see Company B's lease (RLS isolation)");
        }

        // Verify cross-tenant isolation: Company B cannot see Company A's lease
        using (var dbB = CreateContext(companyB, userB))
        {
            await using var txB = await dbB.Database.BeginTransactionAsync();
            var contractAUnderB = await dbB.LeaseContracts
                .FirstOrDefaultAsync(c => c.Id == cA_1);
            contractAUnderB.Should().BeNull(
                "Company B context must not see Company A's lease (RLS isolation)");
        }

        // Verify occupancy trigger fired for all Company A apartments and Company B.
        // Each block needs an explicit transaction so TenantSessionInterceptor fires
        // SET LOCAL app.current_company_id and RLS allows the query.
        using (var dbA = CreateContext(companyA, userA))
        {
            await using var txA = await dbA.Database.BeginTransactionAsync();
            var aptA1Entity = await dbA.Apartments.FirstAsync(a => a.Id == aptA1);
            aptA1Entity.OccupancyStatus.Should().Be(OccupancyStatus.Vacant,
                "PostgreSQL trigger should have set apartment A1 to Vacant after expiration");
            var aptA2Entity = await dbA.Apartments.FirstAsync(a => a.Id == aptA2);
            aptA2Entity.OccupancyStatus.Should().Be(OccupancyStatus.Vacant,
                "PostgreSQL trigger should have set apartment A2 to Vacant after expiration");
            var aptA3Entity = await dbA.Apartments.FirstAsync(a => a.Id == aptA3);
            aptA3Entity.OccupancyStatus.Should().Be(OccupancyStatus.Vacant,
                "PostgreSQL trigger should have set apartment A3 to Vacant after expiration");
            await txA.RollbackAsync();
        }

        using (var dbB = CreateContext(companyB, userB))
        {
            await using var txB = await dbB.Database.BeginTransactionAsync();
            var aptBEntity = await dbB.Apartments.FirstAsync(a => a.Id == aptB);
            aptBEntity.OccupancyStatus.Should().Be(OccupancyStatus.Vacant,
                "PostgreSQL trigger should have set apartment B to Vacant after expiration");
            await txB.RollbackAsync();
        }
    }

    // =========================================================================
    // Test 4: Session leakage test
    //
    // Proves that PostgreSQL connection-pooled sessions do not retain
    // app.current_company_id from a previous scope after SET LOCAL is used.
    //
    // This test exercises the exact sequence:
    //   Company A scope → establish A → transaction → query → commit → dispose
    //   Company B scope → establish B → transaction → query → commit → dispose
    //
    // Verifies that B cannot see A's data and A cannot see B's data across
    // scope boundaries, even when Npgsql reuses the same physical connection.
    // =========================================================================

    [Fact]
    public async Task SessionLeakage_CompanyAScope_DoesNotLeakToCompanyBScope()
    {
        // Arrange: Company A has 3 active expiring leases, Company B has 1
        var (companyA, bldA, aptA, tenantA, userA) = await SeedPropertyHierarchyAsync();
        var (companyB, bldB, aptB, tenantB, userB) = await SeedPropertyHierarchyAsync();

        var contractAId = await SeedEligibleActiveLeaseAsync(companyA, bldA, aptA, tenantA, userA, "LC-LEAK-A");
        var contractBId = await SeedEligibleActiveLeaseAsync(companyB, bldB, aptB, tenantB, userB, "LC-LEAK-B");

        // -----------------------------------------------------------------------
        // Scope A: read contracts under Company A, then fully close the scope.
        // This should reset SET LOCAL app.current_company_id via COMMIT.
        // -----------------------------------------------------------------------
        List<Guid> scopeAResults;
        using (var dbA = CreateContext(companyA, userA))
        {
            await using var txA = await dbA.Database.BeginTransactionAsync();
            // TenantSessionInterceptor fires: SET LOCAL app.current_company_id = companyA
            scopeAResults = await dbA.LeaseContracts.Select(c => c.Id).ToListAsync();
            await txA.CommitAsync();
            // COMMIT resets SET LOCAL — session variable is cleared
        }
        // scope A connection returned to pool; any subsequent reuse starts clean

        // -----------------------------------------------------------------------
        // Scope B: read contracts under Company B. If SET LOCAL leaked from Scope A,
        // Scope B would see Company A's data or no data (wrong company_id).
        // -----------------------------------------------------------------------
        List<Guid> scopeBResults;
        using (var dbB = CreateContext(companyB, userB))
        {
            await using var txB = await dbB.Database.BeginTransactionAsync();
            // TenantSessionInterceptor fires: SET LOCAL app.current_company_id = companyB
            scopeBResults = await dbB.LeaseContracts.Select(c => c.Id).ToListAsync();
            await txB.CommitAsync();
        }

        // Assert: each scope saw only its own company's contracts
        scopeAResults.Should().Contain(contractAId, "Scope A should see Company A's lease");
        scopeAResults.Should().NotContain(contractBId, "Scope A must not see Company B's lease");

        scopeBResults.Should().Contain(contractBId, "Scope B should see Company B's lease");
        scopeBResults.Should().NotContain(contractAId, "Scope B must not see Company A's lease after SET LOCAL reset");
    }

    // =========================================================================
    // Test 5: Platform Admin Security Verification
    //
    // Proves:
    //  A. propertyos_app + normal company scope: SELECTs its own company, not other companies.
    //  B. propertyos_app + platform-admin scope: SELECTs multiple companies across tenants.
    //  C. propertyos_app without company/platform-admin scope: cannot enumerate companies.
    //  D. Platform-admin SELECT policy does NOT grant UPDATE/INSERT/DELETE access across companies.
    // =========================================================================

    [Fact]
    public async Task CompanyRls_PlatformAdminSecurityVerification_EnforcesSelectOnlyAndScopeBoundaries()
    {
        var (companyA, _, _, _, userA) = await SeedPropertyHierarchyAsync();
        var (companyB, _, _, _, userB) = await SeedPropertyHierarchyAsync();

        // A. Normal company scope: can SELECT companyA, cannot SELECT companyB
        using (var dbA = CreateContext(companyA, userA))
        {
            await using var tx = await dbA.Database.BeginTransactionAsync();
            var companiesA = await dbA.Companies.Select(c => c.Id).ToListAsync();
            companiesA.Should().Contain(companyA);
            companiesA.Should().NotContain(companyB);
            await tx.RollbackAsync();
        }

        // B. Platform-admin scope: can SELECT both companies under app_user connection
        using (var dbAdmin = CreateContext(null, isPlatformAdmin: true))
        {
            await using var tx = await dbAdmin.Database.BeginTransactionAsync();
            var allCompanies = await dbAdmin.Companies.Select(c => c.Id).ToListAsync();
            allCompanies.Should().Contain(companyA);
            allCompanies.Should().Contain(companyB);
            await tx.RollbackAsync();
        }

        // C. No company scope + No platform admin: cannot enumerate companies
        using (var dbNone = CreateContext(null, isPlatformAdmin: false))
        {
            await using var tx = await dbNone.Database.BeginTransactionAsync();
            var noCompanies = await dbNone.Companies.Select(c => c.Id).ToListAsync();
            noCompanies.Should().BeEmpty("unauthenticated/unscoped tenant context must return zero rows");
            await tx.RollbackAsync();
        }

        // D. Platform-admin SELECT policy does NOT grant UPDATE/DELETE across companies
        using (var dbAdmin = CreateContext(null, isPlatformAdmin: true))
        {
            await using var tx = await dbAdmin.Database.BeginTransactionAsync();
            // Attempting to update companyB when company context is not set (only platform admin)
            // will affect 0 rows because companies_platform_admin_select_policy is FOR SELECT ONLY
            var rowsAffected = await dbAdmin.Companies
                .Where(c => c.Id == companyB)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.DisplayName, "Unauthorized Update"));

            rowsAffected.Should().Be(0, "platform-admin policy is SELECT-only and must reject cross-tenant UPDATEs");
            await tx.RollbackAsync();
        }
    }
}
