using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationTemplate;

public class UpdateNotificationTemplateCommandHandler : IRequestHandler<UpdateNotificationTemplateCommand>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateNotificationTemplateCommandHandler(
        INotificationTemplateRepository templateRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _templateRepository = templateRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task Handle(UpdateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var template = await _templateRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (template == null)
            throw new KeyNotFoundException($"Notification template with ID '{request.Id}' was not found.");

        if (template.TemplateName != request.TemplateName)
        {
            var exists = await _templateRepository.ExistsByNameAsync(request.TemplateName, companyId, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"A template with name '{request.TemplateName}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        template.Update(
            templateName: request.TemplateName,
            subject: request.Subject,
            body: request.Body,
            updatedAt: now,
            updatedBy: userId
        );

        if (!request.IsActive && template.IsActive)
        {
            template.Deactivate(now, userId);
        }
        else if (request.IsActive && !template.IsActive)
        {
            // Note: The Domain API does not provide an Activate method.
            // If reactivating is genuinely required, a domain specification update is needed.
            throw new InvalidOperationException("Reactivating a deactivated template is not supported by the domain.");
        }

        await _templateRepository.UpdateAsync(template, cancellationToken);
    }
}
