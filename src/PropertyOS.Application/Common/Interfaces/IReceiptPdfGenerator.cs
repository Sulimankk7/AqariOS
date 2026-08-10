using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Common.Interfaces;

public interface IReceiptPdfGenerator
{
    Task<byte[]> GenerateReceiptPdfAsync(RentPayment rentPayment, CancellationToken cancellationToken);
}
