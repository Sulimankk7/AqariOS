using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetMyNotifications;

/// <summary>
/// Keyset-paginated user inbox query. Cursor: (CreatedAt DESC, Id ASC) — pass both
/// values from the last row of the previous page, or neither for the first page.
/// </summary>
public record GetMyNotificationsQuery(
    DateTimeOffset? LastSeenCreatedAt = null,
    Guid? LastSeenId = null,
    int PageSize = 50
) : IRequest<List<NotificationDto>>;
