using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Queries.Common;

namespace PropertyOS.Application.Notifications.Queries.GetNotificationTemplates;

public class GetNotificationTemplatesQueryHandler : IRequestHandler<GetNotificationTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITenantContext _tenantContext;

    public GetNotificationTemplatesQueryHandler(INotificationTemplateRepository templateRepository, ITenantContext tenantContext)
    {
        _templateRepository = templateRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<NotificationTemplateDto>> Handle(GetNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        
        var templates = await _templateRepository.GetTemplatesAsync(companyId, cancellationToken);
        
        return templates.Select(t => new NotificationTemplateDto(
            t.Id,
            t.TemplateName,
            t.Subject,
            t.Body,
            t.NotificationType,
            t.IsActive
        )).ToList();
    }
}
