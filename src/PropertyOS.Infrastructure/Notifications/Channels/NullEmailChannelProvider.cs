using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Notifications.Services;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Channels;

/// <summary>
/// Honest placeholder for the email channel until a real SMTP/provider gateway and
/// credentials exist (mirrors the NullEfawateercomGateway philosophy). Always reports
/// NotConfigured, so email deliveries fail with an explicit reason instead of
/// pretending to have been sent.
/// </summary>
public sealed class NullEmailChannelProvider : INotificationChannelProvider
{
    public DeliveryChannel Channel => DeliveryChannel.Email;

    public Task<ChannelSendResult> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ChannelSendResult.NotConfigured("email"));
    }
}
