using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Leasing;

[Collection("Postgres collection")]
public class TenantAccountIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public TenantAccountIntegrationTests(PostgresTestFixture fixture)
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

    private PropertyOsDbContext CreateContext(Guid? companyId, Guid? userId = null, bool isPlatformAdmin = false)
    {
        var tenantContext = new StaticTenantContext { CompanyId = companyId, IsPlatformAdmin = isPlatformAdmin };
        var userContext = new StaticCurrentUserContext { UserId = userId };

        var connection = isPlatformAdmin 
            ? _fixture.Context.Database.GetDbConnection() 
            : _sharedAppUserConnection!;

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(connection, o => 
            {
                o.MapEnum<CompanyType>("company_type_enum");
                o.MapEnum<LateFeeType>("late_fee_type_enum");
                o.MapEnum<MembershipStatus>("membership_status_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task TenantAccount_EndToEndProvisioningAndIsolation_Enforced()
    {
        Guid companyA_Id = Guid.Empty;
        Guid companyB_Id = Guid.Empty;
        Guid tenantA_Id = Guid.Empty;
        Guid tenantB_Id = Guid.Empty;
        Guid userA_Id = Guid.Empty;

        // 1. Seed Company A, Company B, Tenant A, Tenant B via Admin context
        await using (var adminCtx = CreateContext(null, isPlatformAdmin: true))
        {
            await adminCtx.Database.BeginTransactionAsync();

            var compA = Company.Create("Company A", "Comp A", "+962791110001", CompanyType.PropertyManagementCompany, "JO", DateTimeOffset.UtcNow, null, "CRA", "TAXA", "compa@test.com");
            var compB = Company.Create("Company B", "Comp B", "+962791110002", CompanyType.PropertyManagementCompany, "JO", DateTimeOffset.UtcNow, null, "CRB", "TAXB", "compb@test.com");
            adminCtx.Set<Company>().Add(compA);
            adminCtx.Set<Company>().Add(compB);
            await adminCtx.SaveChangesAsync();

            companyA_Id = compA.Id;
            companyB_Id = compB.Id;

            // Seed System TENANT role
            var tenantRole = new Role
            {
                Id = Guid.CreateVersion7(),
                Code = "TENANT",
                NameEn = "Tenant",
                NameAr = "مستأجر",
                IsSystem = true,
                CompanyId = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            adminCtx.Set<Role>().Add(tenantRole);

            // Create Tenant A under Company A
            var tenantA = Tenant.Create(companyA_Id, "Tenant A Person", "1111111111", "+962791111111", DateTimeOffset.UtcNow, null);
            adminCtx.Set<Tenant>().Add(tenantA);
            tenantA_Id = tenantA.Id;

            // Create Tenant B under Company B
            var tenantB = Tenant.Create(companyB_Id, "Tenant B Person", "2222222222", "+962792222222", DateTimeOffset.UtcNow, null);
            adminCtx.Set<Tenant>().Add(tenantB);
            tenantB_Id = tenantB.Id;

            await adminCtx.SaveChangesAsync();

            // Provision User account for Tenant A under Company A
            userA_Id = Guid.CreateVersion7();
            var userA = new User
            {
                Id = userA_Id,
                FullName = tenantA.Name,
                Phone = tenantA.Phone,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            adminCtx.Set<User>().Add(userA);

            tenantA.LinkUser(userA_Id);

            var membershipA = new UserCompanyRole
            {
                Id = Guid.CreateVersion7(),
                UserId = userA_Id,
                CompanyId = companyA_Id,
                RoleId = tenantRole.Id,
                Status = MembershipStatus.Active,
                InvitedAt = DateTimeOffset.UtcNow,
                JoinedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            adminCtx.Set<UserCompanyRole>().Add(membershipA);

            await adminCtx.SaveChangesAsync();
            await adminCtx.Database.CommitTransactionAsync();
        }

        // 2. Query as Tenant A (Company A scope) -> Should observe Tenant A details
        await using (var ctxA = CreateContext(companyA_Id, userId: userA_Id))
        {
            await ctxA.Database.BeginTransactionAsync();

            var tenant = await ctxA.Set<Tenant>().FirstOrDefaultAsync(t => t.UserId == userA_Id && t.CompanyId == companyA_Id);
            tenant.Should().NotBeNull();
            tenant!.Id.Should().Be(tenantA_Id);
            tenant.Name.Should().Be("Tenant A Person");

            await ctxA.Database.RollbackTransactionAsync();
        }

        // 3. Query as Company B scope -> Cross-company query for Tenant A MUST fail / return null due to PostgreSQL RLS
        await using (var ctxB = CreateContext(companyB_Id, userId: userA_Id))
        {
            await ctxB.Database.BeginTransactionAsync();

            var crossTenant = await ctxB.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == tenantA_Id);
            crossTenant.Should().BeNull(); // RLS isolates Tenant A from Company B

            await ctxB.Database.RollbackTransactionAsync();
        }
    }
}
