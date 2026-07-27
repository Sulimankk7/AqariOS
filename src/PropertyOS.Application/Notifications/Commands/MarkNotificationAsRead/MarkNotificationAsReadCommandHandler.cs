using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException();

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, companyId, cancellationToken);

        // Recipient mismatch is masked as not-found so foreign notifications are indistinguishable from missing ones.
        if (notification == null || notification.RecipientUserId != userId)
            throw new NotFoundException($"Notification '{request.NotificationId}' not found for the current user.");

        try
        {
            notification.MarkAsRead(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: only sent notifications can be marked as read
            throw new BusinessRuleException(ex.Message, "NOTIFICATION_READ_INVALID_STATE");
        }

        await _notificationRepository.UpdateAsync(notification, cancellationToken);

        return Unit.Value;
    }
}
