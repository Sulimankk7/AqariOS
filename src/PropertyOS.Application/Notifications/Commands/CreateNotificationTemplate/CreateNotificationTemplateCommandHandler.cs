using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.CreateNotificationTemplate;

public class CreateNotificationTemplateCommandHandler : IRequestHandler<CreateNotificationTemplateCommand, Guid>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateNotificationTemplateCommandHandler(
        INotificationTemplateRepository templateRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _templateRepository = templateRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var exists = await _templateRepository.ExistsByNameAsync(request.TemplateName, companyId, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"A template with name '{request.TemplateName}' already exists.");

        var now = DateTimeOffset.UtcNow;
        var template = NotificationTemplate.Create(
            companyId: companyId,
            templateName: request.TemplateName,
            notificationType: request.NotificationType,
            subject: request.Subject,
            body: request.Body,
            createdAt: now,
            createdBy: _currentUserContext.UserId
        );

        if (!request.IsActive)
        {
            template.Deactivate(now, _currentUserContext.UserId);
        }

        await _templateRepository.AddAsync(template, cancellationToken);

        return template.Id;
    }
}
