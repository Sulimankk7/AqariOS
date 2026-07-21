using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.UpdateViewingRequestStatus;

public class UpdateViewingRequestStatusCommandValidator : AbstractValidator<UpdateViewingRequestStatusCommand>
{
    public UpdateViewingRequestStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Viewing request ID must be specified.");
    }
}
