using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications.Commands.DeleteNotificationTemplate;

public class DeleteNotificationTemplateCommandHandler : IRequestHandler<DeleteNotificationTemplateCommand, Unit>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteNotificationTemplateCommandHandler(
        INotificationTemplateRepository templateRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _templateRepository = templateRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var template = await _templateRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (template == null)
            throw new NotFoundException($"Notification template with ID '{request.Id}' was not found.");

        // Domain method for soft delete usually updates DeletedAt, DeletedBy
        // However, EF Core handles it automatically with interceptors.
        await _templateRepository.RemoveAsync(template, cancellationToken);

        return Unit.Value;
    }
}
