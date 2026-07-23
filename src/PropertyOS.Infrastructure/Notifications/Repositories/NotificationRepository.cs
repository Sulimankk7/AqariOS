using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Notifications;
using PropertyOS.Domain.Notifications;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Notifications.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly PropertyOsDbContext _context;

    public NotificationRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId, cancellationToken);
    }

    public async Task<Notification?> GetByIdWithDeliveriesAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId, cancellationToken);
    }

    public async Task<NotificationDelivery?> GetDeliveryByIdAsync(Guid deliveryId, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.NotificationDeliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.CompanyId == companyId, cancellationToken);
    }

    public async Task<System.Collections.Generic.List<Notification>> GetUserNotificationsAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.CompanyId == companyId && n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(n => n.CompanyId == companyId && n.RecipientUserId == userId && n.ReadAt == null && n.Status == Domain.Notifications.Enums.NotificationStatus.Sent, cancellationToken);
    }

    public async Task<System.Collections.Generic.List<Notification>> GetCompanyNotificationsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.CompanyId == companyId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<System.Collections.Generic.List<NotificationDelivery>> GetFailedDeliveriesAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.NotificationDeliveries
            .Where(d => d.CompanyId == companyId && d.DeliveryStatus == Domain.Notifications.Enums.DeliveryStatus.Failed)
            .OrderByDescending(d => d.SentAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        _context.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}
