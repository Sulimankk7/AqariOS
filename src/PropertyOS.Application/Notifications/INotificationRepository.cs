using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Notifications;

namespace PropertyOS.Application.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken);
    Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken);
    Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken);
    Task<System.Collections.Generic.List<Notification>> GetUserNotificationsAsync(Guid userId, Guid companyId, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken);
    Task<System.Collections.Generic.List<Notification>> GetCompanyNotificationsAsync(Guid companyId, CancellationToken cancellationToken);
    Task<System.Collections.Generic.List<NotificationDelivery>> GetFailedDeliveriesAsync(Guid companyId, CancellationToken cancellationToken);
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);
}
