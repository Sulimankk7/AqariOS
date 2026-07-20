using System;
using FluentValidation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;

public class ReceiveEfawateercomCallbackCommandValidator : AbstractValidator<ReceiveEfawateercomCallbackCommand>
{
    private static readonly EfawateercomStatus[] ValidCallbackStatuses =
    {
        EfawateercomStatus.Success,
        EfawateercomStatus.Failed,
        EfawateercomStatus.Timeout,
        EfawateercomStatus.Cancelled
    };

    public ReceiveEfawateercomCallbackCommandValidator()
    {
        RuleFor(x => x.ExternalTransactionId)
            .NotEmpty()
            .WithMessage("External transaction ID must be specified.")
            .MaximumLength(100)
            .WithMessage("External transaction ID must not exceed 100 characters.");

        RuleFor(x => x.ResponseTime)
            .NotEmpty()
            .WithMessage("Response time must be specified.")
            .Must(rt => rt != default)
            .WithMessage("Response time must be a valid timestamp.");

        // A callback must carry a terminal status.
        // Pending and Sent are internal-only states; a gateway callback cannot set them.
        RuleFor(x => x.Status)
            .Must(s => Array.IndexOf(ValidCallbackStatuses, s) >= 0)
            .WithMessage(
                "Callback status must be one of: Success, Failed, Timeout, Cancelled. " +
                "Pending and Sent are internal states and cannot be set via callback.");

        // On success, the response must include at least a ResponseCode.
        RuleFor(x => x.ResponseCode)
            .NotEmpty()
            .When(x => x.Status == EfawateercomStatus.Success)
            .WithMessage("ResponseCode must be provided for a successful callback.");
    }
}
