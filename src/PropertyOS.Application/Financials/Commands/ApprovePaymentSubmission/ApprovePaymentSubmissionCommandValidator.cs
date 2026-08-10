using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;

public class ApprovePaymentSubmissionCommandValidator : AbstractValidator<ApprovePaymentSubmissionCommand>
{
    public ApprovePaymentSubmissionCommandValidator()
    {
        RuleFor(v => v.SubmissionId)
            .NotEmpty().WithMessage("Submission ID is required.");
    }
}
