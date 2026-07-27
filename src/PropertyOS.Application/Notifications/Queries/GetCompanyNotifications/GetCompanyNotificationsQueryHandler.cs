using System;
using System.Collections.Generic;
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

        return await _notificationRepository.GetCompanyNotificationsAsync(
            companyId,
            request.PageSize,
            request.LastSeenCreatedAt,
            request.LastSeenId,
            cancellationToken);
    }
}
