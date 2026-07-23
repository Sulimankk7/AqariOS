using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetCompanyNotifications;

public class GetCompanyNotificationsQueryHandler : IRequestHandler<GetCompanyNotificationsQuery, List<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;

    public GetCompanyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<NotificationDto>> Handle(GetCompanyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var notifications = await _notificationRepository.GetCompanyNotificationsAsync(companyId, cancellationToken);

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
