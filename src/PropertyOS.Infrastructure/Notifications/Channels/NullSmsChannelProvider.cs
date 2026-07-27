using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Notifications.Services;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Channels;

/// <summary>
/// Honest placeholder for the SMS channel until a real SMS gateway and credentials
/// exist (mirrors the NullEfawateercomGateway philosophy). Always reports
/// NotConfigured, so SMS deliveries fail with an explicit reason instead of
/// pretending to have been sent.
/// </summary>
public sealed class NullSmsChannelProvider : INotificationChannelProvider
{
    public DeliveryChannel Channel => DeliveryChannel.Sms;

    public Task<ChannelSendResult> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ChannelSendResult.NotConfigured("sms"));
    }
}
