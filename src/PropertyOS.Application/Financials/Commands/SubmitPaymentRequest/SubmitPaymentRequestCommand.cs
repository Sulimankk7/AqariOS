using System;
using MediatR;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;

public record SubmitPaymentRequestCommand(
    Guid RentPaymentId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    Guid? ProofFileId,
    ChequeSubmissionInput? ChequeDetails = null) : IRequest<Guid>;
