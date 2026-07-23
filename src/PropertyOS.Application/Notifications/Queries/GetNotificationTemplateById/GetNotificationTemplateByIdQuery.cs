using System;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetNotificationTemplateById;

public record GetNotificationTemplateByIdQuery(Guid Id) : IRequest<NotificationTemplateDto?>;
