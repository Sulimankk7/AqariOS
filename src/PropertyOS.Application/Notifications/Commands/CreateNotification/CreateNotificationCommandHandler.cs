using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationTemplateRepository templateRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _notificationRepository = notificationRepository;
        _templateRepository = templateRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        if (request.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(request.TemplateId.Value, companyId, cancellationToken);
            if (template == null)
                throw new NotFoundException($"Template '{request.TemplateId}' not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var notification = Notification.Create(
            companyId: companyId,
            recipientUserId: request.RecipientUserId,
            templateId: request.TemplateId,
            notificationType: request.NotificationType,
            subject: request.Subject,
            body: request.Body,
            priority: request.Priority,
            createdAt: now,
            createdBy: _currentUserContext.UserId ?? Guid.Empty
        );
        foreach (var channel in request.Channels)
        {
            try
            {
                notification.AddDeliveryChannel(
                    channel: channel,
                    createdAt: now
                );
            }
            catch (InvalidOperationException ex)
            {
                // Domain rule: the same delivery channel cannot be added twice
                throw new BusinessRuleException(ex.Message, "NOTIFICATION_DELIVERY_CHANNEL_DUPLICATE");
            }
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);

        return notification.Id;
    }
}
