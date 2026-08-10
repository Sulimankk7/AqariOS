using System;
using MediatR;

namespace PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;

public record ApprovePaymentSubmissionCommand(
    Guid SubmissionId) : IRequest;
