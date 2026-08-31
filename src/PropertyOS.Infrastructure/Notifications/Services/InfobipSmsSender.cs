using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.Notifications.Options;

namespace PropertyOS.Infrastructure.Notifications.Services;

/// <summary>
/// Infobip SMS API v3 implementation for AqariOS transactional SMS.
/// </summary>
public sealed class InfobipSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly InfobipOptions _infobipOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<InfobipSmsSender> _logger;

    public InfobipSmsSender(
        HttpClient httpClient,
        IOptions<InfobipOptions> infobipOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<InfobipSmsSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _infobipOptions = infobipOptions?.Value ?? throw new ArgumentNullException(nameof(infobipOptions));
        _frontendOptions = frontendOptions?.Value ?? throw new ArgumentNullException(nameof(frontendOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(
        string to,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeE164(to, out var destination))
        {
            _logger.LogWarning("Infobip SMS was not sent because the destination is not a valid E.164 phone number.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(message) || message.Length > 1600)
        {
            _logger.LogWarning("Infobip SMS was not sent because the message is empty or exceeds the allowed provider-integration length.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_infobipOptions.ApiKey))
        {
            _logger.LogWarning("Infobip SMS was not sent because Infobip:ApiKey is not configured.");
            return false;
        }

        if (!TryGetBaseUri(_infobipOptions.BaseUrl, out _))
        {
            _logger.LogWarning("Infobip SMS was not sent because Infobip:BaseUrl is missing, invalid, or not HTTPS.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_infobipOptions.Sender) ||
            _infobipOptions.Sender.Contains('\r') ||
            _infobipOptions.Sender.Contains('\n'))
        {
            _logger.LogWarning("Infobip SMS was not sent because Infobip:Sender is missing or invalid.");
            return false;
        }

        var payload = new InfobipSmsRequest
        {
            Messages = new[]
            {
                new InfobipSmsMessage
                {
                    Sender = _infobipOptions.Sender.Trim(),
                    Destinations = new[] { new InfobipSmsDestination { To = destination } },
                    Content = new InfobipSmsContent { Text = message.Trim() }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "sms/3/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("App", _infobipOptions.ApiKey.Trim());
        request.Content = JsonContent.Create(payload);

        var maskedPhone = MaskPhone(to);
        try
        {
            _logger.LogInformation("Submitting transactional SMS to Infobip. Destination={DestinationMasked}", maskedPhone);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorType = await ReadSafeErrorTypeAsync(response, cancellationToken);
                _logger.LogWarning(
                    "Infobip rejected transactional SMS. Destination={DestinationMasked} StatusCode={StatusCode} ErrorType={ErrorType}",
                    maskedPhone,
                    (int)response.StatusCode,
                    errorType);
                return false;
            }

            if (!await IsAcceptedResponseAsync(response, cancellationToken))
            {
                _logger.LogWarning(
                    "Infobip returned a malformed SMS acceptance response. Destination={DestinationMasked} StatusCode={StatusCode}",
                    maskedPhone,
                    (int)response.StatusCode);
                return false;
            }

            _logger.LogInformation(
                "Infobip accepted transactional SMS. Destination={DestinationMasked} StatusCode={StatusCode}",
                maskedPhone,
                (int)response.StatusCode);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Infobip SMS request timed out. Destination={DestinationMasked}", maskedPhone);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                "Infobip SMS network request failed. Destination={DestinationMasked} ExceptionType={ExceptionType}",
                maskedPhone,
                ex.GetType().Name);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                "Unexpected Infobip SMS failure. Destination={DestinationMasked} ExceptionType={ExceptionType}",
                maskedPhone,
                ex.GetType().Name);
            return false;
        }
    }

    public Task<bool> SendTenantActivationSmsAsync(
        string recipientPhone,
        string tenantName,
        string activationToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantName) || string.IsNullOrWhiteSpace(activationToken))
        {
            _logger.LogWarning("Tenant activation SMS was not sent because required data is missing.");
            return Task.FromResult(false);
        }

        var baseUrl = string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl)
            ? "http://localhost:5173"
            : _frontendOptions.BaseUrl.TrimEnd('/');
        var activationUrl = $"{baseUrl}/auth/activate?token={Uri.EscapeDataString(activationToken.Trim())}";
        var message = $"مرحباً {tenantName.Trim()}، فعّل حسابك في AqariOS وأنشئ كلمة المرور عبر الرابط: {activationUrl}";

        return SendAsync(recipientPhone, message, cancellationToken);
    }

    private static async Task<bool> IsAcceptedResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array ||
                messages.GetArrayLength() == 0)
            {
                return false;
            }

            var firstMessage = messages[0];
            return firstMessage.ValueKind == JsonValueKind.Object &&
                   firstMessage.TryGetProperty("messageId", out var messageId) &&
                   !string.IsNullOrWhiteSpace(messageId.GetString());
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task<string> ReadSafeErrorTypeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("requestError", out var requestError) &&
                requestError.TryGetProperty("serviceException", out var serviceException) &&
                serviceException.TryGetProperty("messageId", out var messageId))
            {
                return ToSafeErrorType(messageId.GetString());
            }
        }
        catch (JsonException)
        {
            // Raw provider responses are deliberately not logged or returned.
        }

        return "provider_error";
    }

    private static string ToSafeErrorType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80)
            return "provider_error";

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character) && character is not '_' and not '-')
                return "provider_error";
        }

        return value;
    }

    private static bool TryNormalizeE164(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var phone = value.Trim();
        if (phone.Length < 9 || phone.Length > 16 || phone[0] != '+')
            return false;

        for (var index = 1; index < phone.Length; index++)
        {
            if (!char.IsAsciiDigit(phone[index]))
                return false;
        }

        if (phone[1] == '0')
            return false;

        // Preserve the caller's international E.164 representation for Infobip's v3 payload.
        normalized = phone;
        return true;
    }

    private static bool TryGetBaseUri(string? value, out Uri? baseUri)
    {
        baseUri = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        baseUri = uri;
        return true;
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "[EMPTY]";

        var value = phone.Trim();
        return value.Length <= 6 ? "***" : $"{value[..4]}****{value[^3..]}";
    }

    private sealed class InfobipSmsRequest
    {
        [JsonPropertyName("messages")]
        public InfobipSmsMessage[] Messages { get; init; } = Array.Empty<InfobipSmsMessage>();
    }

    private sealed class InfobipSmsMessage
    {
        [JsonPropertyName("sender")]
        public string Sender { get; init; } = string.Empty;

        [JsonPropertyName("destinations")]
        public InfobipSmsDestination[] Destinations { get; init; } = Array.Empty<InfobipSmsDestination>();

        [JsonPropertyName("content")]
        public InfobipSmsContent Content { get; init; } = new();
    }

    private sealed class InfobipSmsDestination
    {
        [JsonPropertyName("to")]
        public string To { get; init; } = string.Empty;
    }

    private sealed class InfobipSmsContent
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;
    }
}
