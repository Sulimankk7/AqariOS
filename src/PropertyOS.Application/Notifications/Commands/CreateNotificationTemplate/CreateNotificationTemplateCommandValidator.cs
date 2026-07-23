using FluentValidation;

namespace PropertyOS.Application.Notifications.Commands.CreateNotificationTemplate;

public class CreateNotificationTemplateCommandValidator : AbstractValidator<CreateNotificationTemplateCommand>
{
    public CreateNotificationTemplateCommandValidator()
    {
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
