using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Services;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Commands.DispatchNotification;

/// <summary>
/// Delivery dispatch for one notification.
///
/// DISPATCHABLE STATES (derived from the domain guards):
///   • Cancelled or soft-deleted   → never dispatched (silent no-op; MarkAsSent throws
///                                    from Cancelled, and Cancelled/deleted rows must
///                                    not produce sends).
///   • Pending                     → always processed.
///   • Sent / Failed               → processed only when at least one delivery is still
///                                    sendable; otherwise no-op (Sent is sticky — the
///                                    domain forbids Sent → Failed; Failed with every
///                                    delivery exhausted is terminal).
///
/// SENDABLE DELIVERIES: DeliveryStatus.Pending, or Failed with
/// AttemptCount &lt; NotificationDispatchPolicy.MaxDeliveryAttempts (the domain permits
/// re-sending a Failed delivery — MarkAsSent has no status guard, only the
/// AttemptCount &gt; 0 guard — but defines no attempt cap, so the cap is app policy).
/// RecordAttempt is called BEFORE the provider send, so the AttemptCount guards of
/// MarkAsSent / MarkAsFailed always hold afterwards; any InvalidOperationException from
/// the domain here would therefore be a defect in this handler's own sequencing and is
/// deliberately NOT swallowed (surfaces as 500, not 422).
///
/// PARENT TRANSITION (after processing deliveries):
///   • ≥1 delivery Sent/Delivered                        → MarkAsSent (idempotent).
///   • no deliveries at all, or every delivery Failed at
///     the attempt cap (and parent not already Sent)     → MarkAsFailed (terminal).
///   • otherwise                                          → unchanged; retryable Failed
///     deliveries keep the notification eligible for the next sweep.
///
/// TransactionBehavior owns the transaction and the single SaveChangesAsync —
/// this handler never calls SaveChanges.
/// </summary>
public class DispatchNotificationCommandHandler : IRequestHandler<DispatchNotificationCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IReadOnlyDictionary<DeliveryChannel, INotificationChannelProvider> _providersByChannel;

    public DispatchNotificationCommandHandler(
        INotificationRepository notificationRepository,
        ITenantContext tenantContext,
        IEnumerable<INotificationChannelProvider> channelProviders)
    {
        _notificationRepository = notificationRepository;
        _tenantContext = tenantContext;

        var providersByChannel = new Dictionary<DeliveryChannel, INotificationChannelProvider>();
        foreach (var provider in channelProviders)
        {
            // Indexer (not Add): a duplicate registration must not turn every dispatch
            // into a handler-construction failure — the last registration wins.
            providersByChannel[provider.Channel] = provider;
        }

        _providersByChannel = providersByChannel;
    }

    public async Task<Unit> Handle(DispatchNotificationCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var notification = await _notificationRepository.GetByIdWithDeliveriesAsync(
            request.NotificationId, companyId, cancellationToken);
        if (notification == null)
            throw new NotFoundException($"Notification '{request.NotificationId}' not found.");

        // Never dispatch cancelled or soft-deleted notifications — silent no-op.
        if (notification.DeletedAt.HasValue || notification.Status == NotificationStatus.Cancelled)
            return Unit.Value;

        var sendableDeliveries = notification.Deliveries.Where(IsSendable).ToList();

        // Sent with nothing left to retry, or Failed with every channel exhausted:
        // terminal — silent no-op. (Pending falls through so the parent status can be
        // settled even when no delivery is sendable, e.g. a channel-less notification.)
        if (sendableDeliveries.Count == 0 && notification.Status != NotificationStatus.Pending)
            return Unit.Value;

        var now = DateTimeOffset.UtcNow;

        foreach (var delivery in sendableDeliveries)
        {
            // Attempt is recorded up front (we are about to try): the domain requires
            // AttemptCount > 0 before MarkAsSent/MarkAsFailed, and a not-configured
            // channel must still consume an attempt so it converges to terminal Failed
            // instead of being swept forever.
            delivery.RecordAttempt(now);

            ChannelSendResult result;
            if (!_providersByChannel.TryGetValue(delivery.DeliveryChannel, out var provider))
            {
                result = ChannelSendResult.NotConfigured(delivery.DeliveryChannel.ToString());
            }
            else
            {
                try
                {
                    result = await provider.SendAsync(notification, delivery, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Providers are expected to report failures via ChannelSendResult;
                    // a throwing provider is downgraded to a failed attempt so one
                    // channel cannot abort the other channels of the same notification.
                    result = ChannelSendResult.Failed(ex.Message);
                }
            }

            if (result.Success)
            {
                delivery.MarkAsSent(now);
            }
            else
            {
                var reason = string.IsNullOrWhiteSpace(result.FailureReason)
                    ? "Unknown delivery failure."
                    : result.FailureReason!;
                delivery.MarkAsFailed(reason, now);
            }
        }

        // Parent transition — guards proven by the early returns above: the notification
        // is not Cancelled (MarkAsSent cannot throw) and MarkAsFailed is only reached
        // when the status is not Sent (the domain forbids Sent → Failed).
        if (notification.Deliveries.Any(d =>
                d.DeliveryStatus is DeliveryStatus.Sent or DeliveryStatus.Delivered))
        {
            notification.MarkAsSent(now);
        }
        else if (notification.Status != NotificationStatus.Sent &&
                 (notification.Deliveries.Count == 0 ||
                  notification.Deliveries.All(d =>
                      d.DeliveryStatus == DeliveryStatus.Failed &&
                      d.AttemptCount >= NotificationDispatchPolicy.MaxDeliveryAttempts)))
        {
            notification.MarkAsFailed(now);
        }
        // else: retryable failures remain — leave the status for the next sweep.

        await _notificationRepository.UpdateAsync(notification, cancellationToken);

        return Unit.Value;
    }

    private static bool IsSendable(NotificationDelivery delivery) =>
        delivery.DeliveryStatus == DeliveryStatus.Pending ||
        (delivery.DeliveryStatus == DeliveryStatus.Failed &&
         delivery.AttemptCount < NotificationDispatchPolicy.MaxDeliveryAttempts);
}
