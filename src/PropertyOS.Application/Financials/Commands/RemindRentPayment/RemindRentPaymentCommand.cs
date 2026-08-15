using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.RemindRentPayment;

/// <summary>
/// Command to send an outstanding rent payment reminder to the associated tenant.
/// </summary>
public record RemindRentPaymentCommand(Guid RentPaymentId) : ICommand<RemindRentPaymentResponseDto>;
