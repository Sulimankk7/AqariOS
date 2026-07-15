using System;
using System.Collections.Generic;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;

public record AllocationDetail(Guid ObligationPaymentId, decimal Amount);

public record RecordPaymentAllocationCommand(
    Guid ReceivingPaymentId,
    List<AllocationDetail> Allocations,
    DateOnly AllocationDate,
    string? Notes = null) : ICommand;
