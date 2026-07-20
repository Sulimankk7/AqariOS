using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Financials.Repositories;

public class CompanyReceiptSequenceRepository : ICompanyReceiptSequenceRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public CompanyReceiptSequenceRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CompanyReceiptSequence?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CompanyReceiptSequences
            .FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken);
    }

    public async Task AddAsync(CompanyReceiptSequence sequence, CancellationToken cancellationToken = default)
    {
        await _dbContext.CompanyReceiptSequences.AddAsync(sequence, cancellationToken);
    }

    public async Task<long> ReserveNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        if (_dbContext.Database.CurrentTransaction != null)
            command.Transaction = _dbContext.Database.CurrentTransaction.GetDbTransaction();

        command.CommandText = @"
            UPDATE company_receipt_sequences
            SET current_number = CASE 
                    WHEN reset_policy = 'yearly' AND DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) THEN 1
                    WHEN reset_policy = 'monthly' AND (DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) 
                         OR DATE_PART('month', now()) != COALESCE(DATE_PART('month', last_reset_at), 0)) THEN 1
                    ELSE current_number + 1
                END,
                last_reset_at = CASE
                    WHEN reset_policy = 'yearly' AND DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) THEN now()
                    WHEN reset_policy = 'monthly' AND (DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) 
                         OR DATE_PART('month', now()) != COALESCE(DATE_PART('month', last_reset_at), 0)) THEN now()
                    ELSE last_reset_at
                END,
                updated_at = now()
            WHERE company_id = @companyId
            RETURNING current_number;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@companyId";
        parameter.Value = companyId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null)
            throw new InvalidOperationException($"Receipt sequence configuration not found for company '{companyId}'.");

        return Convert.ToInt64(result);
    }

    public async Task<string> ReserveAndFormatNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        if (_dbContext.Database.CurrentTransaction != null)
            command.Transaction = _dbContext.Database.CurrentTransaction.GetDbTransaction();

        // Single atomic UPDATE: lock row, evaluate reset, increment, return formatting parameters.
        command.CommandText = @"
            UPDATE company_receipt_sequences
            SET current_number = CASE 
                    WHEN reset_policy = 'yearly' AND DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) THEN 1
                    WHEN reset_policy = 'monthly' AND (DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) 
                         OR DATE_PART('month', now()) != COALESCE(DATE_PART('month', last_reset_at), 0)) THEN 1
                    ELSE current_number + 1
                END,
                last_reset_at = CASE
                    WHEN reset_policy = 'yearly' AND DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) THEN now()
                    WHEN reset_policy = 'monthly' AND (DATE_PART('year', now()) != COALESCE(DATE_PART('year', last_reset_at), 0) 
                         OR DATE_PART('month', now()) != COALESCE(DATE_PART('month', last_reset_at), 0)) THEN now()
                    ELSE last_reset_at
                END,
                updated_at = now()
            WHERE company_id = @companyId
            RETURNING current_number, prefix, padding_length;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@companyId";
        parameter.Value = companyId;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"Receipt sequence configuration not found for company '{companyId}'.");

        var currentNumber = Convert.ToInt64(reader["current_number"]);
        var prefix        = reader["prefix"] as string ?? string.Empty;
        var paddingLength = Convert.ToInt16(reader["padding_length"]);

        // Format: prefix + zero-padded number. Matches CompanyReceiptSequence.FormatReceiptNumber().
        return $"{prefix}{currentNumber.ToString().PadLeft(paddingLength, '0')}";
    }
}
