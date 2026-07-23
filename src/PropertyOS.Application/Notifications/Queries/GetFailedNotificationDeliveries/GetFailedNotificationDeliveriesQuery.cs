using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetFailedNotificationDeliveries;

public record GetFailedNotificationDeliveriesQuery : IRequest<List<NotificationDeliveryDto>>;
