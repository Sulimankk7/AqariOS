using FluentValidation;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationTemplate;

public class UpdateNotificationTemplateCommandValidator : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();

        RuleFor(v => v.TemplateName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(v => v.Subject)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(v => v.Body)
            .NotEmpty();

        RuleFor(v => v.NotificationType)
            .IsInEnum();
    }
}
