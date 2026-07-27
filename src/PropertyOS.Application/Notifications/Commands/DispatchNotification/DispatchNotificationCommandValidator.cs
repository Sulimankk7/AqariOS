using FluentValidation;

namespace PropertyOS.Application.Notifications.Commands.DispatchNotification;

public class DispatchNotificationCommandValidator : AbstractValidator<DispatchNotificationCommand>
{
    public DispatchNotificationCommandValidator()
    {
        RuleFor(v => v.NotificationId).NotEmpty();
    }
}
