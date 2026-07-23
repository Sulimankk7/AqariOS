using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery : IRequest<List<NotificationDto>>;
