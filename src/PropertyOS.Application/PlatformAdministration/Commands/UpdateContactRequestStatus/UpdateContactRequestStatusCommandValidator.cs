using FluentValidation;
using PropertyOS.Domain.PlatformAdministration.Enums;
namespace PropertyOS.Application.PlatformAdministration.Commands.UpdateContactRequestStatus;
public sealed class UpdateContactRequestStatusCommandValidator : AbstractValidator<UpdateContactRequestStatusCommand>
{
    public UpdateContactRequestStatusCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Status).NotEmpty().Must(x => Enum.TryParse<ContactRequestStatus>(x, true, out _)).WithMessage("Invalid contact request status."); }
}
