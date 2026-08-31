using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

[Collection("Postgres collection")]
public abstract class Module4ApiTestBase : IAsyncLifetime, IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly PostgresTestFixture Fixture;
    protected readonly WebApplicationFactory<Program> Factory;

    protected Module4ApiTestBase(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
    {
        Fixture = fixture;
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = fixture.RawConnectionString,
                    ["Jwt:Secret"] = "integration-test-only-jwt-secret-at-least-32-bytes",
                    ["Otp:HashKey"] = "integration-test-only-otp-hmac-key-at-least-32-bytes"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PropertyOsDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<PropertyOsDbContext>((sp, options) =>
                {
                    options.UseNpgsql(fixture.DataSource, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(PropertyOsDbContext).Assembly.FullName);
                    });
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    protected HttpClient CreateClientWithPermissions(Guid userId, Guid companyId, params string[] permissions)
    {
        var client = Factory.CreateClient();
        using var scope = Factory.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        var (token, _) = tokenGenerator.GenerateAccessToken(userId, companyId, new[] { "COMPANY_ADMIN" }, permissions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected HttpClient CreateClientWithoutToken()
    {
        return Factory.CreateClient();
    }

    protected async Task<(Guid CompanyId, Guid UserId)> CreateTestTenantAsync(string namePrefix = "Tenant")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();

        var now = DateTimeOffset.UtcNow;
        var crNo = $"CR{Guid.NewGuid().ToString("N")[..8]}";
        var taxNo = $"TAX{Guid.NewGuid().ToString("N")[..8]}";
        var phone = $"+96279{Random.Shared.Next(1000000, 9999999)}";

        // Use the domain factory method Company.Create(...)
        var company = Company.Create(
            legalName: $"{namePrefix} Legal Ltd",
            displayName: $"{namePrefix} Corp",
            primaryPhone: phone,
            companyType: CompanyType.PropertyManagementCompany,
            countryCode: "JO",
            createdAt: now,
            createdBy: null,
            commercialRegistrationNo: crNo,
            taxNumber: taxNo,
            primaryEmail: $"{namePrefix.ToLower()}_{Guid.NewGuid().ToString("N")[..6]}@test.com"
        );

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var companyId = company.Id;

        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            Email = $"{namePrefix.ToLower()}_user_{Guid.NewGuid().ToString("N")[..6]}@test.com",
            FullName = $"{namePrefix} Admin User",
            PasswordHash = "AQAAAAEAACcQAAAAE...",
            PasswordAlgorithm = "argon2id",
            PreferredLanguage = "en",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(user);

        var adminRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = "COMPANY_ADMIN",
            NameEn = "Company Administrator",
            NameAr = "مدير الشركة",
            IsSystem = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Roles.Add(adminRole);

        var membership = new UserCompanyRole
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CompanyId = companyId,
            RoleId = adminRole.Id,
            Status = PropertyOS.Domain.Identity.Enums.MembershipStatus.Active,
            CreatedAt = now
        };
        db.UserCompanyRoles.Add(membership);

        await db.SaveChangesAsync();
        return (companyId, userId);
    }
}
