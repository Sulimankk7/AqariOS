using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationDelivery;

public class UpdateNotificationDeliveryCommandHandler : IRequestHandler<UpdateNotificationDeliveryCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateNotificationDeliveryCommandHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task Handle(UpdateNotificationDeliveryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var notification = await _notificationRepository.GetByIdWithDeliveriesAsync(request.NotificationId, companyId, cancellationToken);
        if (notification == null)
            throw new KeyNotFoundException($"Notification '{request.NotificationId}' not found.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId ?? Guid.Empty; // System user typically updates this

        var delivery = notification.Deliveries.FirstOrDefault(d => d.Id == request.DeliveryId);
        if (delivery == null)
            throw new KeyNotFoundException($"Delivery '{request.DeliveryId}' not found for this notification.");

        switch (request.NewStatus)
        {
            case Domain.Notifications.Enums.DeliveryStatus.Sent:
                delivery.MarkAsSent(now);
                break;
            case Domain.Notifications.Enums.DeliveryStatus.Delivered:
                delivery.MarkAsDelivered(now);
                break;
            case Domain.Notifications.Enums.DeliveryStatus.Failed:
                // TODO: Specification Issue #1 - chk_notification_deliveries_sent_at_requires_status 
                // requires sent_at for failed. The specification is currently ambiguous.
                // Keeping implementation aligned with the domain behavior, intentionally allowing 
                // the database constraint violation to surface until an explicit decision is made.
                delivery.MarkAsFailed(request.FailureReason ?? "Unknown Error", now);
                break;
            default:
                throw new InvalidOperationException($"Invalid state transition to {request.NewStatus}");
        }

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
