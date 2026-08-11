using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Financials.Repositories;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace PropertyOS.Tests.Integration.Infrastructure.Financials;

[Collection("Postgres collection")]
public class PendingPaymentVerificationQueryTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public PendingPaymentVerificationQueryTests(PostgresTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPendingVerificationsAsync_ExecutesWithoutPostgresEnumErrors()
    {
        // Arrange
        var repository = new RentPaymentRepository(_fixture.Context);
        
        var companyId = Guid.CreateVersion7();

        // Act
        // This will throw a PostgresException (42883: operator does not exist: submission_status_enum = integer)
        // if the PostgreSQL enum is not properly mapped in the EF Core model.
        var result = await repository.GetPendingVerificationsAsync(
            companyId,
            pageSize: 50,
            lastSeenSubmittedAt: null,
            lastSeenId: null,
            cancellationToken: default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }
}
