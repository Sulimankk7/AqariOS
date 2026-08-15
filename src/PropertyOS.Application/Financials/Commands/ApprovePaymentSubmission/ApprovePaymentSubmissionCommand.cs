using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;

public record ApprovePaymentSubmissionCommand(
    Guid SubmissionId) : ICommand;

