using System;
using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.AddMaintenanceAttachment;

public class AddMaintenanceAttachmentCommandValidator : AbstractValidator<AddMaintenanceAttachmentCommand>
{
    public AddMaintenanceAttachmentCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        // FileId is optional until the File storage module exists,
        // but when provided it must be a real identifier.
        RuleFor(x => x.FileId)
            .NotEqual(Guid.Empty)
            .When(x => x.FileId.HasValue)
            .WithMessage("File ID must be specified when provided.");

        RuleFor(x => x.Description)
            .MaximumLength(255)
            .WithMessage("Description must not exceed 255 characters.");
    }
}
