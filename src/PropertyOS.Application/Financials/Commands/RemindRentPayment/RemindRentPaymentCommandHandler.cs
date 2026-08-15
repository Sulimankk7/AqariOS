using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Financials.Commands.RemindRentPayment;

/// <summary>
/// Handles sending payment reminder notifications to tenants for outstanding rent obligations.
/// Follows strict multi-tenant scoping, state validation, and post-commit dispatch guarantees.
/// </summary>
public class RemindRentPaymentCommandHandler : IRequestHandler<RemindRentPaymentCommand, RemindRentPaymentResponseDto>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly ISender _mediator;
    private readonly ILogger<RemindRentPaymentCommandHandler> _logger;

    public RemindRentPaymentCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ITenantRepository tenantRepository,
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IPostCommitRegistrar postCommitRegistrar,
        ISender mediator,
        ILogger<RemindRentPaymentCommandHandler> logger)
    {
        _rentPaymentRepository = rentPaymentRepository ?? throw new ArgumentNullException(nameof(rentPaymentRepository));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _postCommitRegistrar = postCommitRegistrar ?? throw new ArgumentNullException(nameof(postCommitRegistrar));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RemindRentPaymentResponseDto> Handle(RemindRentPaymentCommand request, CancellationToken cancellationToken)
    {
        // ── 1. Company Scope ─────────────────────────────────────────────────────────────
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // ── 2. Load Rent Payment ────────────────────────────────────────────────────────
        var payment = await _rentPaymentRepository.GetByIdAsync(request.RentPaymentId, cancellationToken);
        if (payment == null || payment.DeletedAt.HasValue || payment.CompanyId != companyId)
        {
            _logger.LogWarning(
                "Rent payment reminder rejected: payment not found or cross-tenant. RentPaymentId={RentPaymentId} CompanyId={CompanyId}",
                request.RentPaymentId, companyId);
            throw new NotFoundException($"Rent payment with ID '{request.RentPaymentId}' was not found.");
        }

        // ── 3. Payment State Validation ──────────────────────────────────────────────────
        if (payment.DueDateStatus == DueDateStatus.Cancelled)
        {
            throw new BusinessRuleException(
                "Cannot send a reminder for a cancelled rent payment.",
                "PAYMENT_CANCELLED");
        }

        if (payment.DueDateStatus == DueDateStatus.Paid)
        {
            throw new BusinessRuleException(
                "Cannot send a reminder for a fully paid rent payment.",
                "PAYMENT_ALREADY_PAID");
        }

        var remainingAmount = payment.AmountDue - payment.AmountPaid;
        if (remainingAmount <= 0)
        {
            throw new BusinessRuleException(
                "Cannot send a reminder for a rent payment with no outstanding balance.",
                "PAYMENT_FULLY_SETTLED");
        }

        // ── 4. Load Associated Tenant ────────────────────────────────────────────────────
        var tenant = await _tenantRepository.GetByIdAsync(payment.TenantId, cancellationToken);
        if (tenant == null || tenant.DeletedAt.HasValue || tenant.CompanyId != companyId)
        {
            _logger.LogWarning(
                "Rent payment reminder rejected: associated tenant not found or cross-tenant. TenantId={TenantId} CompanyId={CompanyId}",
                payment.TenantId, companyId);
            throw new NotFoundException($"Tenant with ID '{payment.TenantId}' was not found.");
        }

        // ── 5. Tenant User Account Resolution ───────────────────────────────────────────
        if (!tenant.UserId.HasValue || tenant.UserId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Rent payment reminder rejected: tenant has no linked user account. TenantId={TenantId}",
                tenant.Id);
            throw new BusinessRuleException(
                "The tenant does not have an active user account to receive reminders.",
                "TENANT_ACCOUNT_UNAVAILABLE");
        }

        var now = DateTimeOffset.UtcNow;
        var isOverdue = payment.DueDateStatus is DueDateStatus.Late or DueDateStatus.OverdueUnpaid;
        var notificationType = isOverdue ? NotificationType.LatePayment : NotificationType.RentDue;
        var priority = isOverdue ? NotificationPriority.High : NotificationPriority.Normal;

        var subject = isOverdue
            ? "تذكير: دفعة إيجار متأخرة | Overdue Rent Payment Reminder"
            : "تذكير: استحقاق دفعة الإيجار | Rent Payment Due Reminder";

        var body = isOverdue
            ? $"نود تذكيركم بوجود دفعة إيجار متأخرة بمبلغ {remainingAmount:N2} {payment.Currency}. يرجى مراجعة البوابة لتسديد الدفعة. | Please note that you have an overdue rent payment of {remainingAmount:N2} {payment.Currency}. Please visit the portal to complete payment."
            : $"نود تذكيركم بموعد استحقاق دفعة الإيجار بمبلغ {remainingAmount:N2} {payment.Currency}. يرجى مراجعة البوابة لتسديد الدفعة. | Please be reminded that your rent payment of {remainingAmount:N2} {payment.Currency} is due. Please visit the portal to complete payment.";

        // ── 6. Create Notification Entity (Module 11) ────────────────────────────────────
        var notification = Notification.Create(
            companyId: companyId,
            recipientUserId: tenant.UserId.Value,
            templateId: null,
            notificationType: notificationType,
            subject: subject,
            body: body,
            priority: priority,
            createdAt: now,
            createdBy: _currentUserContext.UserId
        );

        // Always register InApp channel for the recipient user
        notification.AddDeliveryChannel(DeliveryChannel.InApp, now);

        if (!string.IsNullOrWhiteSpace(tenant.Email))
        {
            notification.AddDeliveryChannel(DeliveryChannel.Email, now);
        }

        if (!string.IsNullOrWhiteSpace(tenant.Phone))
        {
            notification.AddDeliveryChannel(DeliveryChannel.Sms, now);
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);

        _logger.LogInformation(
            "Created rent payment reminder notification. NotificationId={NotificationId} RecipientUserId={RecipientUserId} RentPaymentId={RentPaymentId}",
            notification.Id, tenant.UserId.Value, payment.Id);

        // ── 7. Register Post-Commit Dispatch Action ─────────────────────────────────────
        var notificationId = notification.Id;
        _postCommitRegistrar.RegisterPostCommitAction(async (ct) =>
        {
            _logger.LogInformation(
                "Executing post-commit dispatch for rent payment reminder. NotificationId={NotificationId} RentPaymentId={RentPaymentId}",
                notificationId, payment.Id);

            await _mediator.Send(new DispatchNotificationCommand(notificationId), ct);
        });

        return new RemindRentPaymentResponseDto(
            RentPaymentId: payment.Id,
            NotificationId: notification.Id,
            Status: "Accepted",
            Message: "Payment reminder created and queued for dispatch."
        );
    }
}
