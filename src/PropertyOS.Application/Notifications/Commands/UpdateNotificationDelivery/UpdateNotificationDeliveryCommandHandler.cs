using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationDelivery;

public class UpdateNotificationDeliveryCommandHandler : IRequestHandler<UpdateNotificationDeliveryCommand, Unit>
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

    public async Task<Unit> Handle(UpdateNotificationDeliveryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var notification = await _notificationRepository.GetByIdWithDeliveriesAsync(request.NotificationId, companyId, cancellationToken);
        if (notification == null)
            throw new NotFoundException($"Notification '{request.NotificationId}' not found.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId ?? Guid.Empty; // System user typically updates this

        var delivery = notification.Deliveries.FirstOrDefault(d => d.Id == request.DeliveryId);
        if (delivery == null)
            throw new NotFoundException($"Delivery '{request.DeliveryId}' not found for this notification.");

        try
        {
            switch (request.NewStatus)
            {
                case Domain.Notifications.Enums.DeliveryStatus.Sent:
                    delivery.MarkAsSent(now);
                    break;
                case Domain.Notifications.Enums.DeliveryStatus.Delivered:
                    delivery.MarkAsDelivered(now);
                    break;
                case Domain.Notifications.Enums.DeliveryStatus.Failed:
                    delivery.MarkAsFailed(request.FailureReason ?? "Unknown Error", now);
                    break;
                default:
                    throw new BusinessRuleException($"Invalid state transition to {request.NewStatus}", "NOTIFICATION_DELIVERY_INVALID_TRANSITION");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Domain state machine rejected the transition
            throw new BusinessRuleException(ex.Message, "NOTIFICATION_DELIVERY_INVALID_TRANSITION");
        }

        await _notificationRepository.UpdateAsync(notification, cancellationToken);

        return Unit.Value;
    }
}
