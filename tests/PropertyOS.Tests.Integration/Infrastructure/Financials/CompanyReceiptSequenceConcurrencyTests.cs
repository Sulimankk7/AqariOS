using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Financials;

[Collection("Postgres collection")]
public class CompanyReceiptSequenceConcurrencyTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public CompanyReceiptSequenceConcurrencyTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ReserveAndFormatNextReceiptNumberAsync_ConcurrentExecutions_ProduceGaplessSequentialPaddedNumbers()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        // Seed sequence settings as admin
        await using (var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
                VALUES (@cId, 'Concurrency Co', 'Concurrency', '+962790000000', 'individual_owner', now(), now());

                INSERT INTO company_receipt_sequences (id, company_id, prefix, padding_length, reset_policy, current_number, last_reset_at, created_at, updated_at)
                VALUES (@sId, @cId, 'INV-', 6, 'never', 0, NULL, now(), now());
            ";
            cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
            cmd.Parameters.Add(new NpgsqlParameter("sId", Guid.NewGuid()));
            await cmd.ExecuteNonQueryAsync();
        }

        var sequenceRepository = new PropertyOS.Infrastructure.Financials.Repositories.CompanyReceiptSequenceRepository(_fixture.Context);

        var count = 100;
        var list = new ConcurrentBag<string>();
        var tasks = new List<Task>();
        using var semaphore = new SemaphoreSlim(15);

        // Act
        // Execute 100 concurrent requests to reserve and format a receipt number
        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // Each task gets its own transaction/connection context to simulate concurrent API traffic
                    var dbContextOptions = new DbContextOptionsBuilder<PropertyOsDbContext>()
                        .UseNpgsql(_fixture.RawConnectionString, o =>
                        {
                            o.MapEnum<CompanyType>("company_type_enum");
                            o.MapEnum<LateFeeType>("late_fee_type_enum");
                            o.MapEnum<ReceiptResetPolicy>("receipt_reset_policy_enum");
                        })
                        .Options;

                    using var localContext = new PropertyOsDbContext(dbContextOptions);
                    var localRepository = new PropertyOS.Infrastructure.Financials.Repositories.CompanyReceiptSequenceRepository(localContext);

                    // Run inside a database transaction to simulate the command transactional boundary (TransactionBehavior)
                    await using var transaction = await localContext.Database.BeginTransactionAsync();
                    try
                    {
                        var formattedNumber = await localRepository.ReserveAndFormatNextReceiptNumberAsync(companyId);
                        list.Add(formattedNumber);
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var results = list.ToList();

        // 1. Verify we received exactly 100 results
        Assert.Equal(count, results.Count);

        // 2. Verify all numbers have the correct prefix and padding (e.g. "INV-000001")
        Assert.All(results, num => Assert.StartsWith("INV-", num));
        Assert.All(results, num => Assert.Equal(10, num.Length)); // "INV-" (4 chars) + 6 padded digits

        // 3. Extract the integer sequences and verify they are sequential, unique, and gapless (1 to 100)
        var sequenceNumbers = results
            .Select(num => int.Parse(num.Substring(4)))
            .OrderBy(n => n)
            .ToList();

        for (int i = 1; i <= count; i++)
        {
            Assert.Equal(i, sequenceNumbers[i - 1]);
        }
    }
}
