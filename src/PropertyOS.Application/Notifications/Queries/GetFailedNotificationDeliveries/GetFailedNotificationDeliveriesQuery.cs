using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetFailedNotificationDeliveries;

/// <summary>
/// Keyset-paginated failed-delivery query. Cursor: (SentAt DESC, Id ASC) — a Failed
/// delivery always has a non-null SentAt. Pass both values from the last row of the
/// previous page, or neither for the first page.
/// </summary>
public record GetFailedNotificationDeliveriesQuery(
    DateTimeOffset? LastSeenSentAt = null,
    Guid? LastSeenId = null,
    int PageSize = 50
) : IRequest<List<NotificationDeliveryDto>>;
