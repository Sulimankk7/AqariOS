using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand>
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

    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException();

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, companyId, cancellationToken);
        
        if (notification == null || notification.RecipientUserId != userId)
            throw new KeyNotFoundException($"Notification '{request.NotificationId}' not found for the current user.");

        notification.MarkAsRead(DateTimeOffset.UtcNow);

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
