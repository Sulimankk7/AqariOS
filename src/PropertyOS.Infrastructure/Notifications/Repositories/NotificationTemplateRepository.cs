using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Notifications;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Notifications.Repositories;

internal sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly PropertyOsDbContext _context;

    public NotificationTemplateRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken);
    }

    public async Task<System.Collections.Generic.List<NotificationTemplate>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.NotificationTemplates
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.TemplateName)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationTemplate?> GetByCompanyAndTypeAsync(Guid companyId, NotificationType notificationType, CancellationToken cancellationToken)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.NotificationType == notificationType && t.IsActive, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string templateName, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.NotificationTemplates
            .AnyAsync(t => t.CompanyId == companyId && t.TemplateName == templateName, cancellationToken);
    }

    public Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        _context.NotificationTemplates.Add(template);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        _context.NotificationTemplates.Update(template);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        // Handled as Soft Delete by EF Interceptor when marked Deleted
        _context.NotificationTemplates.Remove(template);
        return Task.CompletedTask;
    }
}
