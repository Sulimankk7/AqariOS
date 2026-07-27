using System;
using System.Collections.Generic;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for the cheque block of a manual cheque payment.
/// Required when <see cref="RecordManualRentPaymentRequest.Method"/> is <see cref="PaymentMethod.Cheque"/>.
/// </summary>
public record ManualChequeDetailsRequest(
    string ChequeNumber,
    string BankName,
    string? BankBranch,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateOnly? ReceivedDate = null
);

/// <summary>
/// Request model for recording a manual money-in rent payment (Cash / BankTransfer / Cheque).
/// </summary>
public record RecordManualRentPaymentRequest(
    Guid LeaseContractId,
    decimal Amount,
    PaymentMethod Method,
    string? PaymentReferenceNumber = null,
    string? Notes = null,
    ManualChequeDetailsRequest? Cheque = null,
    List<AllocationDetailRequest>? Allocations = null
);
