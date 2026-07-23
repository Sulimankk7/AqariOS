using FluentValidation;

namespace PropertyOS.Application.Files.Commands.UploadFileRequest;

public class UploadFileRequestCommandValidator : AbstractValidator<UploadFileRequestCommand>
{
    public UploadFileRequestCommandValidator()
    {
        RuleFor(x => x.ModuleName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Filename).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
    }
}
