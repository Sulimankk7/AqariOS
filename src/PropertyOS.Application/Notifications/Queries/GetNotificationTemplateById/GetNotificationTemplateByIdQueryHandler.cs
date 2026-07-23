using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetNotificationTemplateById;

public class GetNotificationTemplateByIdQueryHandler : IRequestHandler<GetNotificationTemplateByIdQuery, NotificationTemplateDto?>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITenantContext _tenantContext;

    public GetNotificationTemplateByIdQueryHandler(INotificationTemplateRepository templateRepository, ITenantContext tenantContext)
    {
        _templateRepository = templateRepository;
        _tenantContext = tenantContext;
    }

    public async Task<NotificationTemplateDto?> Handle(GetNotificationTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        
        var template = await _templateRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        
        if (template == null) return null;

        return new NotificationTemplateDto(
            template.Id,
            template.TemplateName,
            template.Subject,
            template.Body,
            template.NotificationType,
            template.IsActive
        );
    }
}
