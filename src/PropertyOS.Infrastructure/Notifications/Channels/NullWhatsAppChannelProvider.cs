using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Notifications.Services;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Channels;

/// <summary>
/// Honest placeholder for the WhatsApp channel until a real WhatsApp Business API
/// gateway and credentials exist (mirrors the NullEfawateercomGateway philosophy).
/// Always reports NotConfigured, so WhatsApp deliveries fail with an explicit reason
/// instead of pretending to have been sent.
/// </summary>
public sealed class NullWhatsAppChannelProvider : INotificationChannelProvider
{
    public DeliveryChannel Channel => DeliveryChannel.WhatsApp;

    public Task<ChannelSendResult> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ChannelSendResult.NotConfigured("whatsapp"));
    }
}
