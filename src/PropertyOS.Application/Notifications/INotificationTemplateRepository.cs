using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken);
    Task<NotificationTemplate?> GetByCompanyAndTypeAsync(Guid companyId, NotificationType notificationType, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string templateName, Guid companyId, CancellationToken cancellationToken);
    Task<System.Collections.Generic.List<NotificationTemplate>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken);
    Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken);
    Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken);
    Task RemoveAsync(NotificationTemplate template, CancellationToken cancellationToken);
}
