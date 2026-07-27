using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetCompanyNotifications;

/// <summary>
/// Keyset-paginated company-wide notification query (administrative view).
/// Cursor: (CreatedAt DESC, Id ASC) — pass both values from the last row of the
/// previous page, or neither for the first page.
/// </summary>
public record GetCompanyNotificationsQuery(
    DateTimeOffset? LastSeenCreatedAt = null,
    Guid? LastSeenId = null,
    int PageSize = 50
) : IRequest<List<NotificationDto>>;
