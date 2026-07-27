namespace PropertyOS.Application.Notifications.Services;

/// <summary>
/// Outcome of a single channel send attempt. Providers never throw for expected
/// delivery failures — they report them here so the dispatch pipeline can record
/// the attempt and apply the domain's delivery state machine.
/// </summary>
public record ChannelSendResult(bool Success, string? FailureReason)
{
    public static ChannelSendResult Ok() => new(true, null);

    /// <summary>
    /// The channel has no real gateway wired up (mirrors the NullEfawateercomGateway
    /// philosophy: honest placeholder until credentials/configuration exist).
    /// </summary>
    public static ChannelSendResult NotConfigured(string channelName) =>
        new(false, $"No {channelName} delivery gateway is configured.");

    public static ChannelSendResult Failed(string reason) => new(false, reason);
}
