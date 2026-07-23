using FluentValidation;

namespace PropertyOS.Application.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(v => v.RecipientUserId).NotEmpty();

        RuleFor(v => v.Subject)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(v => v.Body)
            .NotEmpty();

        RuleFor(v => v.NotificationType)
            .IsInEnum();

        RuleFor(v => v.Priority)
            .IsInEnum();

        RuleFor(v => v.Channels)
            .NotEmpty()
            .WithMessage("At least one delivery channel is required.");
    }
}
