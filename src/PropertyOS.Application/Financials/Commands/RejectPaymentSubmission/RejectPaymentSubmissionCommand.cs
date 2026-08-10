using System;
using MediatR;

namespace PropertyOS.Application.Financials.Commands.RejectPaymentSubmission;

public record RejectPaymentSubmissionCommand(
    Guid SubmissionId,
    string Reason) : IRequest;
