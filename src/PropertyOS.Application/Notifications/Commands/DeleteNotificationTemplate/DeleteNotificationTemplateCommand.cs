using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Notifications.Commands.DeleteNotificationTemplate;

public record DeleteNotificationTemplateCommand(Guid Id) : ICommand;
