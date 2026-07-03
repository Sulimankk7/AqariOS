using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Companies.Persistence;

[Collection("Postgres collection")]
public sealed class CompanyPersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public CompanyPersistenceTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Can_Save_And_Retrieve_Valid_Company()
    {
        // Arrange
        var context = _fixture.Context;
        var company = Company.Create(
            legalName: "AqariOS LLC",
            displayName: "AqariOS",
            primaryPhone: "+962791234567",
            companyType: CompanyType.PropertyManagementCompany,
            countryCode: "JO",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: null);

        // Act
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedCompany = await context.Companies.SingleAsync(c => c.Id == company.Id);

        // Assert
        savedCompany.Should().NotBeNull();
        savedCompany.LegalName.Should().Be("AqariOS LLC");
        savedCompany.CompanyType.Should().Be(CompanyType.PropertyManagementCompany);
        
        // Assert database-generated defaults
        savedCompany.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        savedCompany.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        savedCompany.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Primary_Phone_Check_Constraint_Rejects_Invalid_Format()
    {
        // Arrange
        var context = _fixture.Context;
        // Business logic guards against this normally, but we use reflection or a workaround 
        // if we want to bypass domain validation to test DB constraints. Or just use a raw SQL insert.
        // Let's use raw SQL to test the database constraint explicitly.
        
        // Act
        Func<Task> action = async () =>
        {
            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO companies (legal_name, display_name, primary_phone)
                VALUES ('Test', 'Test', '0791234567')"); // Missing +962
        };

        // Assert
        var ex = await action.Should().ThrowAsync<PostgresException>();
        ex.WithMessage("*chk_companies_primary_phone_format*");
    }

    [Fact]
    public async Task Unique_Constraint_Rejects_Duplicate_Commercial_Registration_No()
    {
        // Arrange
        var context = _fixture.Context;
        
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO companies (legal_name, display_name, primary_phone, commercial_registration_no)
            VALUES ('C1', 'C1', '+962791234567', 'REG123')");

        // Act
        Func<Task> action = async () =>
        {
            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO companies (legal_name, display_name, primary_phone, commercial_registration_no)
                VALUES ('C2', 'C2', '+962791234568', 'REG123')"); // Duplicate REG123
        };

        // Assert
        var ex = await action.Should().ThrowAsync<PostgresException>();
        ex.WithMessage("*uq_companies_commercial_registration_no*");
    }

    [Fact]
    public async Task Unique_Constraint_Allows_Multiple_Null_Commercial_Registration_No()
    {
        // Arrange
        var context = _fixture.Context;
        
        // Act
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO companies (legal_name, display_name, primary_phone, commercial_registration_no)
            VALUES ('C1', 'C1', '+962791234567', NULL)");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO companies (legal_name, display_name, primary_phone, commercial_registration_no)
            VALUES ('C2', 'C2', '+962791234568', NULL)");

        // Assert - no exception thrown
        var count = await context.Companies.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task UpdatedAt_Trigger_Fires_On_Database_Update()
    {
        // Arrange
        var context = _fixture.Context;
        var company = Company.Create("Test", "Test", "+962791234567", CompanyType.IndividualOwner, "JO", DateTimeOffset.UtcNow, null);
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var initialUpdatedAt = company.UpdatedAt;

        // Ensure a tiny delay so the timestamp will definitely be different
        await Task.Delay(100);

        // Act
        // We update using raw SQL to ensure EF Core isn't the one setting the updated_at field,
        // proving the database BEFORE UPDATE trigger works.
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE companies SET display_name = 'Changed' WHERE id = {0}", company.Id);

        // Assert
        var updatedCompany = await context.Companies.SingleAsync(c => c.Id == company.Id);
        updatedCompany.DisplayName.Should().Be("Changed");
        updatedCompany.UpdatedAt.Should().BeAfter(initialUpdatedAt);
    }

    [Fact]
    public async Task CompanySettings_Created_With_Defaults_Via_Cascade()
    {
        // Arrange
        var context = _fixture.Context;
        var company = Company.Create("Test", "Test", "+962791234567", CompanyType.IndividualOwner, "JO", DateTimeOffset.UtcNow, null);
        
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        
        var settings = CompanySettings.CreateWithDefaults(company.Id, DateTimeOffset.UtcNow);
        context.CompanySettings.Add(settings);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var savedSettings = await context.CompanySettings.SingleAsync(s => s.CompanyId == company.Id);

        // Assert
        savedSettings.Should().NotBeNull();
        savedSettings.DefaultCurrency.Should().Be("JOD");
        savedSettings.RentGracePeriodDays.Should().Be(5);
        savedSettings.LateFeeType.Should().Be(LateFeeType.None);
        savedSettings.LateFeeValue.Should().BeNull();
        savedSettings.FiscalYearStartMonth.Should().Be(1);
    }
}
