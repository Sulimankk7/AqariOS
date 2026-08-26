using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Events;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Financials.EventHandlers;

public class RentPaymentRejectedEventHandler : INotificationHandler<DomainEventNotification<RentPaymentRejectedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly ISender _mediator;
    private readonly ILogger<RentPaymentRejectedEventHandler> _logger;

    public RentPaymentRejectedEventHandler(
        IApplicationDbContext context,
        INotificationRepository notificationRepository,
        IPostCommitRegistrar postCommitRegistrar,
        ISender mediator,
        ILogger<RentPaymentRejectedEventHandler> logger)
    {
        _context = context;
        _notificationRepository = notificationRepository;
        _postCommitRegistrar = postCommitRegistrar;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RentPaymentRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        
        var rentPayment = await _context.RentPayments
            .Include(rp => rp.Submissions)
            .FirstOrDefaultAsync(rp => rp.Id == domainEvent.RentPaymentId, cancellationToken);
            
        if (rentPayment == null)
        {
            _logger.LogWarning("RentPayment {Id} not found when handling RentPaymentRejectedEvent", domainEvent.RentPaymentId);
            return;
        }

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == rentPayment.TenantId, cancellationToken);

        if (tenant?.UserId == null)
        {
            _logger.LogWarning(
                "Tenant {TenantId} has no linked UserId; skipping in-app rejection notification for RentPayment {PaymentId}.",
                rentPayment.TenantId, rentPayment.Id);
            return;
        }

        var submission = rentPayment.Submissions.FirstOrDefault(s => s.Id == domainEvent.SubmissionId);
        var submittedAmount = submission?.Amount ?? rentPayment.AmountDue;
        var currency = string.IsNullOrWhiteSpace(rentPayment.Currency) ? "JOD" : rentPayment.Currency;
        var now = DateTimeOffset.UtcNow;

        var subject = "تم رفض إثبات الدفع | Payment Submission Rejected";
        var body = $"تم رفض إثبات الدفع بقيمة {submittedAmount:N2} {currency} للقسط المستحق. سبب الرفض: {domainEvent.Reason} | Your payment submission of {submittedAmount:N2} {currency} was rejected. Reason: {domainEvent.Reason}";

        var notificationEntity = Notification.Create(
            companyId: rentPayment.CompanyId,
            recipientUserId: tenant.UserId.Value,
            templateId: null,
            notificationType: NotificationType.GeneralNotification,
            subject: subject,
            body: body,
            priority: NotificationPriority.High,
            createdAt: now,
            createdBy: domainEvent.RejectedBy
        );

        notificationEntity.AddDeliveryChannel(DeliveryChannel.InApp, now);

        if (!string.IsNullOrWhiteSpace(tenant.Email))
        {
            notificationEntity.AddDeliveryChannel(DeliveryChannel.Email, now);
        }

        if (!string.IsNullOrWhiteSpace(tenant.Phone))
        {
            notificationEntity.AddDeliveryChannel(DeliveryChannel.Sms, now);
        }

        await _notificationRepository.AddAsync(notificationEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created rejection notification {NotificationId} for Tenant {TenantId} (UserId={UserId}) on RentPayment {PaymentId}.",
            notificationEntity.Id, tenant.Id, tenant.UserId.Value, rentPayment.Id);

        var notificationId = notificationEntity.Id;
        _postCommitRegistrar.RegisterPostCommitAction(async (ct) =>
        {
            try
            {
                await _mediator.Send(new DispatchNotificationCommand(notificationId), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed post-commit dispatch for rejection notification {NotificationId}", notificationId);
            }
        });
    }
}
