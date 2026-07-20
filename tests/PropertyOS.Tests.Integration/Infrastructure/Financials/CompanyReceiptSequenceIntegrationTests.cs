using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Financials;

[Collection("Postgres collection")]
public class CompanyReceiptSequenceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public CompanyReceiptSequenceIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ReserveAndFormatNextReceiptNumberAsync_MonthlyResetPolicy_ResetsCorrectly()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var sequenceId = Guid.NewGuid();
        var lastMonth = DateTimeOffset.UtcNow.AddMonths(-1);

        // Seed sequence settings with a monthly reset policy and last_reset_at set to last month
        await using (var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
                VALUES (@cId, 'Monthly Reset Co', 'Monthly Reset', '+962790000000', 'individual_owner', now(), now());

                INSERT INTO company_receipt_sequences (id, company_id, prefix, padding_length, reset_policy, current_number, last_reset_at, created_at, updated_at)
                VALUES (@sId, @cId, 'REC-', 5, 'monthly', 42, @lastReset, now(), now());
            ";
            cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
            cmd.Parameters.Add(new NpgsqlParameter("sId", sequenceId));
            cmd.Parameters.Add(new NpgsqlParameter("lastReset", lastMonth));
            await cmd.ExecuteNonQueryAsync();
        }

        var repository = new PropertyOS.Infrastructure.Financials.Repositories.CompanyReceiptSequenceRepository(_fixture.Context);

        // Act
        // 1. Call to reserve number — since month changed, current_number must reset to 1
        var num1 = await repository.ReserveAndFormatNextReceiptNumberAsync(companyId);

        // 2. Call again — should increment to 2 (no reset since it's the same month)
        var num2 = await repository.ReserveAndFormatNextReceiptNumberAsync(companyId);

        // Assert
        Assert.Equal("REC-00001", num1);
        Assert.Equal("REC-00002", num2);

        // Verify last_reset_at was updated to today
        var updatedSeq = await _fixture.Context.CompanyReceiptSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId);
        Assert.NotNull(updatedSeq);
        Assert.NotNull(updatedSeq.LastResetAt);
        Assert.True(updatedSeq.LastResetAt.Value > lastMonth);
    }

    [Fact]
    public async Task ReserveAndFormatNextReceiptNumberAsync_YearlyResetPolicy_ResetsCorrectly()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var sequenceId = Guid.NewGuid();
        var lastYear = DateTimeOffset.UtcNow.AddYears(-1);

        // Seed sequence settings with a yearly reset policy and last_reset_at set to last year
        await using (var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
                VALUES (@cId, 'Yearly Reset Co', 'Yearly Reset', '+962790000000', 'individual_owner', now(), now());

                INSERT INTO company_receipt_sequences (id, company_id, prefix, padding_length, reset_policy, current_number, last_reset_at, created_at, updated_at)
                VALUES (@sId, @cId, 'REC-', 5, 'yearly', 100, @lastReset, now(), now());
            ";
            cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
            cmd.Parameters.Add(new NpgsqlParameter("sId", sequenceId));
            cmd.Parameters.Add(new NpgsqlParameter("lastReset", lastYear));
            await cmd.ExecuteNonQueryAsync();
        }

        var repository = new PropertyOS.Infrastructure.Financials.Repositories.CompanyReceiptSequenceRepository(_fixture.Context);

        // Act
        var num1 = await repository.ReserveAndFormatNextReceiptNumberAsync(companyId);
        var num2 = await repository.ReserveAndFormatNextReceiptNumberAsync(companyId);

        // Assert
        Assert.Equal("REC-00001", num1);
        Assert.Equal("REC-00002", num2);
    }

    [Fact]
    public async Task ReserveAndFormatNextReceiptNumberAsync_NeverResetPolicy_DoesNotReset()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var sequenceId = Guid.NewGuid();
        var lastYear = DateTimeOffset.UtcNow.AddYears(-1);

        // Seed sequence settings with reset policy set to never
        await using (var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
                VALUES (@cId, 'Never Reset Co', 'Never Reset', '+962790000000', 'individual_owner', now(), now());

                INSERT INTO company_receipt_sequences (id, company_id, prefix, padding_length, reset_policy, current_number, last_reset_at, created_at, updated_at)
                VALUES (@sId, @cId, 'REC-', 5, 'never', 100, @lastReset, now(), now());
            ";
            cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
            cmd.Parameters.Add(new NpgsqlParameter("sId", sequenceId));
            cmd.Parameters.Add(new NpgsqlParameter("lastReset", lastYear));
            await cmd.ExecuteNonQueryAsync();
        }

        var repository = new PropertyOS.Infrastructure.Financials.Repositories.CompanyReceiptSequenceRepository(_fixture.Context);

        // Act
        var num = await repository.ReserveAndFormatNextReceiptNumberAsync(companyId);

        // Assert
        // Should not reset: 100 -> 101
        Assert.Equal("REC-00101", num);
    }
}
