using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceAttachment;

public class RemoveMaintenanceAttachmentCommandValidator : AbstractValidator<RemoveMaintenanceAttachmentCommand>
{
    public RemoveMaintenanceAttachmentCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        RuleFor(x => x.AttachmentId)
            .NotEmpty()
            .WithMessage("Attachment ID must be specified.");
    }
}
