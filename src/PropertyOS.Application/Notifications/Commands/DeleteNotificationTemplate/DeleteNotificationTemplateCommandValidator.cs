using FluentValidation;

namespace PropertyOS.Application.Notifications.Commands.DeleteNotificationTemplate;

public class DeleteNotificationTemplateCommandValidator : AbstractValidator<DeleteNotificationTemplateCommand>
{
    public DeleteNotificationTemplateCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
    }
}
