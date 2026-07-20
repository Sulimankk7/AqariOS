using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials;

public interface ICompanyReceiptSequenceRepository
{
    Task<CompanyReceiptSequence?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task AddAsync(CompanyReceiptSequence sequence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically locks the company sequence row, evaluates reset policy, increments
    /// current_number, and returns the fully-formatted receipt number string (e.g. "INV-0042").
    /// This is a single round-trip and the only correct way to generate a receipt number.
    /// </summary>
    Task<string> ReserveAndFormatNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Low-level number reservation only. Prefer ReserveAndFormatNextReceiptNumberAsync.
    /// Retained for backward compatibility with existing tests and callers.
    /// </summary>
    Task<long> ReserveNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default);
}

