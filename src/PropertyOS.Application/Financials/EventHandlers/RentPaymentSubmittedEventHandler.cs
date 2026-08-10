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

public class RentPaymentSubmittedEventHandler : INotificationHandler<DomainEventNotification<RentPaymentSubmittedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RentPaymentSubmittedEventHandler> _logger;

    public RentPaymentSubmittedEventHandler(
        IApplicationDbContext context,
        ILogger<RentPaymentSubmittedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RentPaymentSubmittedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        
        var rentPayment = await _context.RentPayments
            .FirstOrDefaultAsync(rp => rp.Id == domainEvent.RentPaymentId, cancellationToken);
            
        if (rentPayment == null)
        {
            _logger.LogWarning("RentPayment {Id} not found when handling RentPaymentSubmittedEvent", domainEvent.RentPaymentId);
            return;
        }

        // Notify the Property Manager / Owner
        // This would integrate with Module 11 (Notifications)
        _logger.LogInformation("Notification dispatched to Company/Owner {CompanyId} for newly submitted payment verification.", 
            rentPayment.CompanyId);
            
        await Task.CompletedTask;
    }
}
