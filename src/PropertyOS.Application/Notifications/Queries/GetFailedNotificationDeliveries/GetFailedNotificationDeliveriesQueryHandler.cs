using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetFailedNotificationDeliveries;

public class GetFailedNotificationDeliveriesQueryHandler : IRequestHandler<GetFailedNotificationDeliveriesQuery, List<NotificationDeliveryDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;

    public GetFailedNotificationDeliveriesQueryHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<NotificationDeliveryDto>> Handle(GetFailedNotificationDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var deliveries = await _notificationRepository.GetFailedDeliveriesAsync(companyId, cancellationToken);

        return deliveries.Select(d => new NotificationDeliveryDto(
            d.Id,
            d.NotificationId,
            d.DeliveryChannel,
            d.DeliveryStatus,
            d.AttemptCount,
            d.CreatedAt,
            d.SentAt,
            d.DeliveredAt,
            d.FailureReason
        )).ToList();
    }
}
