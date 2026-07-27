using System;
using System.Collections.Generic;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for allocating a receiving payment across one or more obligation payments.
/// </summary>
public record RecordPaymentAllocationRequest(
    Guid ReceivingPaymentId,
    List<AllocationDetailRequest> Allocations,
    DateOnly AllocationDate,
    string? Notes = null
);
