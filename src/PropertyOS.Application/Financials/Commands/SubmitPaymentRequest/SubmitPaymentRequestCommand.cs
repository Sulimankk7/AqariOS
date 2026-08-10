using System;
using MediatR;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;

public record SubmitPaymentRequestCommand(
    Guid RentPaymentId,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    Guid? ProofFileId) : IRequest<Guid>;
