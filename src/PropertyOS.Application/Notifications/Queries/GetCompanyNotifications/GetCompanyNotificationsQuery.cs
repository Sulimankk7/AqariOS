using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetCompanyNotifications;

public record GetCompanyNotificationsQuery : IRequest<List<NotificationDto>>;
