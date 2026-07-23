using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Notifications.Queries.GetMyUnreadNotificationCount;

public class GetMyUnreadNotificationCountQueryHandler : IRequestHandler<GetMyUnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyUnreadNotificationCountQueryHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<int> Handle(GetMyUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException();

        return await _notificationRepository.GetUnreadCountAsync(userId, companyId, cancellationToken);
    }
}
