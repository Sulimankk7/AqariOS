using FluentValidation;

namespace PropertyOS.Application.PlatformAdministration.Commands.ApproveLandlordRegistration;

public sealed class ApproveLandlordRegistrationCommandValidator : AbstractValidator<ApproveLandlordRegistrationCommand>
{
    public ApproveLandlordRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
    }
}
