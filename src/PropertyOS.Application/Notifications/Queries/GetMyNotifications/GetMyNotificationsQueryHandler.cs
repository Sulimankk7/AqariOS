using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException();

        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, companyId, cancellationToken);

        return notifications.Select(n => new NotificationDto(
            n.Id,
            n.RecipientUserId,
            n.Subject,
            n.Body,
            n.NotificationType,
            n.Priority,
            n.Status,
            n.CreatedAt,
            n.ReadAt
        )).ToList();
    }
}
