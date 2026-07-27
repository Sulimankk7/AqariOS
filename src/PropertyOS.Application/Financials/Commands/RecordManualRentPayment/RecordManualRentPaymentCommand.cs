using System;
using System.Collections.Generic;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordManualRentPayment;

/// <summary>
/// Cheque block for manual cheque payments. Required (validator-enforced) when
/// <see cref="RecordManualRentPaymentCommand.Method"/> is <see cref="PaymentMethod.Cheque"/>.
/// </summary>
public record ManualChequeDetails(
    string ChequeNumber,
    string BankName,
    string? BankBranch,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateOnly? ReceivedDate = null);

/// <summary>
/// Records a manual money-in payment (Cash / BankTransfer / Cheque) against a lease contract
/// as an UnallocatedReceipt. Efawateercom is rejected — gateway money-in flows exclusively
/// through the eFAWATEERcom transaction pipeline. Optional allocations are settled through
/// the existing RecordPaymentAllocationCommand pipeline within the same transaction.
/// </summary>
public record RecordManualRentPaymentCommand(
    Guid LeaseContractId,
    decimal Amount,
    PaymentMethod Method,
    string? PaymentReferenceNumber = null,
    string? Notes = null,
    ManualChequeDetails? Cheque = null,
    List<AllocationDetail>? Allocations = null
) : ICommand<Guid>;
