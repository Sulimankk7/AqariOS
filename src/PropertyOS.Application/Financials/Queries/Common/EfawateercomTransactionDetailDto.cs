using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

/// <summary>
/// EfawateercomTransactionDto extended with raw webhook response payload
/// for the single-record detail endpoint.
/// </summary>
public class EfawateercomTransactionDetailDto : EfawateercomTransactionDto
{
    public string? RawResponse { get; set; }
}
