using System;
using MediatR;

namespace PropertyOS.Application.Notifications.Commands.DeleteNotificationTemplate;

public record DeleteNotificationTemplateCommand(Guid Id) : IRequest;
