using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.CancelRentPayment;

/// <summary>
/// Cancels a rent payment that has no active allocations referencing it
/// (in either direction — as obligation or as receiving payment).
/// </summary>
public record CancelRentPaymentCommand(
    Guid RentPaymentId,
    string? Reason = null
) : ICommand;
