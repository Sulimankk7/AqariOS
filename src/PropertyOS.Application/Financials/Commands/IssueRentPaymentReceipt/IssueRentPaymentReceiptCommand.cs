using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;

/// <summary>
/// Issues the official receipt for a fully-paid rent payment and returns the
/// formatted receipt number (e.g. "REC-00042").
/// </summary>
public record IssueRentPaymentReceiptCommand(Guid RentPaymentId) : ICommand<string>;
