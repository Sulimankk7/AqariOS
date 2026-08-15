using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Notifications.Commands.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, MarkAllNotificationsAsReadResult>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    public async Task<MarkAllNotificationsAsReadResult> Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException();

        var dbTime = await _notificationRepository.GetDatabaseTimestampAsync(cancellationToken);
        if (dbTime == default)
        {
            dbTime = DateTimeOffset.UtcNow;
        }

        var markedCount = await _notificationRepository.MarkAllAsReadAsync(userId, companyId, dbTime, cancellationToken);

        return new MarkAllNotificationsAsReadResult(markedCount);
    }
}
