using System;

namespace PropertyOS.Application.Financials.Commands.RemindRentPayment;

/// <summary>
/// Response payload for rent payment reminder dispatch.
/// </summary>
public record RemindRentPaymentResponseDto(
    Guid RentPaymentId,
    Guid NotificationId,
    string Status,
    string Message
);
