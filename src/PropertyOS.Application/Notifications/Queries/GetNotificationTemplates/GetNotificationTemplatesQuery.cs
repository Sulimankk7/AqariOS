using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetNotificationTemplates;

public record GetNotificationTemplatesQuery : IRequest<List<NotificationTemplateDto>>;
