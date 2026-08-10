using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Domain.Financials.Events;

namespace PropertyOS.Application.Financials.EventHandlers;

public class RentPaymentRejectedEventHandler : INotificationHandler<DomainEventNotification<RentPaymentRejectedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RentPaymentRejectedEventHandler> _logger;

    public RentPaymentRejectedEventHandler(
        IApplicationDbContext context,
        ILogger<RentPaymentRejectedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RentPaymentRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        
        var rentPayment = await _context.RentPayments
            .FirstOrDefaultAsync(rp => rp.Id == domainEvent.RentPaymentId, cancellationToken);
            
        if (rentPayment == null)
        {
            _logger.LogWarning("RentPayment {Id} not found when handling RentPaymentRejectedEvent", domainEvent.RentPaymentId);
            return;
        }

        // Notify the Tenant
        // This would integrate with Module 11 (Notifications) via MediatR Commands like CreateNotificationCommand
        _logger.LogInformation("Notification dispatched to Tenant {TenantId} for REJECTED payment. Reason: {Reason}", 
            rentPayment.TenantId, domainEvent.Reason);
            
        await Task.CompletedTask;
    }
}
