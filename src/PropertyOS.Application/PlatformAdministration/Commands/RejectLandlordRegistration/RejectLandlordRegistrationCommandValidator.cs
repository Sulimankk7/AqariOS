using FluentValidation;

namespace PropertyOS.Application.PlatformAdministration.Commands.RejectLandlordRegistration;

public sealed class RejectLandlordRegistrationCommandValidator : AbstractValidator<RejectLandlordRegistrationCommand>
{
    public RejectLandlordRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Rejection reason is required.")
            .MinimumLength(5)
            .MaximumLength(500);
    }
}
