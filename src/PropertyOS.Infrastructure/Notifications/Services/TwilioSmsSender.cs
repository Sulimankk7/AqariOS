using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.Notifications.Options;

namespace PropertyOS.Infrastructure.Notifications.Services;

/// <summary>
/// Twilio SMS implementation using Twilio REST API and Messaging Service SID.
/// </summary>
// Retained as an isolated legacy implementation. InfobipSmsSender is the active
// ISmsSender registration.
public class TwilioSmsSender
{
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _twilioOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(
        HttpClient httpClient,
        IOptions<TwilioOptions> twilioOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<TwilioSmsSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _twilioOptions = twilioOptions?.Value ?? throw new ArgumentNullException(nameof(twilioOptions));
        _frontendOptions = frontendOptions?.Value ?? throw new ArgumentNullException(nameof(frontendOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendTenantActivationSmsAsync(
        string recipientPhone,
        string tenantName,
        string activationToken,
        CancellationToken cancellationToken = default)
    {
        var maskedPhone = MaskPhone(recipientPhone);
        _logger.LogInformation(
            "[DIAG:TwilioSmsSender] SMS sender invoked. RecipientPhoneMasked={RecipientPhoneMasked}, TenantName={TenantName}",
            maskedPhone, tenantName);

        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            _logger.LogWarning("[DIAG:TwilioSmsSender] Cannot send tenant activation SMS: recipient phone is empty.");
            return false;
        }

        var accountSid = _twilioOptions.AccountSid?.Trim() ?? string.Empty;
        var authToken = _twilioOptions.AuthToken?.Trim() ?? string.Empty;
        var messagingServiceSid = !string.IsNullOrWhiteSpace(_twilioOptions.MessagingServiceSid)
            ? _twilioOptions.MessagingServiceSid.Trim()
            : "MGa02c1d790deba589a8ff733e39faeb37";

        bool hasAccountSid = !string.IsNullOrWhiteSpace(accountSid);
        bool hasAuthToken = !string.IsNullOrWhiteSpace(authToken);

        _logger.LogInformation(
            "[DIAG:TwilioSmsSender] Twilio configuration status: HasAccountSid={HasAccountSid}, HasAuthToken={HasAuthToken}, MessagingServiceSid={MessagingServiceSid}",
            hasAccountSid, hasAuthToken, messagingServiceSid);

        if (!hasAccountSid || !hasAuthToken)
        {
            _logger.LogWarning("[DIAG:TwilioSmsSender] Twilio AccountSid or AuthToken is not configured. Skipping SMS delivery.");
            return false;
        }

        var smsBody = "AqariOS SMS Test";

        var (charCount, encoding, estimatedSegments) = AnalyzeMessage(smsBody);
        _logger.LogInformation(
            "[DIAG:TwilioSmsSender] SMS metrics: CharacterCount={CharCount}, DetectedEncoding={Encoding}, EstimatedSegments={EstimatedSegments}",
            charCount, encoding, estimatedSegments);

        if (estimatedSegments > 1)
        {
            _logger.LogWarning(
                "[DIAG:TwilioSmsSender] SMS message exceeds 1 segment (CharacterCount={CharCount}, EstimatedSegments={EstimatedSegments}). Twilio Trial accounts may reject multi-segment messages with error 30044.",
                charCount, estimatedSegments);
        }

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

        var formParams = new Dictionary<string, string>
        {
            { "To", recipientPhone.Trim() },
            { "MessagingServiceSid", messagingServiceSid },
            { "Body", smsBody }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        request.Content = new FormUrlEncodedContent(formParams);

        try
        {
            _logger.LogInformation("[DIAG:TwilioSmsSender] Dispatching HTTP POST request to Twilio Messages API...");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[DIAG:TwilioSmsSender] Twilio SMS dispatch SUCCESS. StatusCode={StatusCode}, RecipientPhoneMasked={RecipientPhoneMasked}",
                    (int)response.StatusCode, maskedPhone);
                return true;
            }

            _logger.LogWarning(
                "[DIAG:TwilioSmsSender] Twilio SMS dispatch FAILED. StatusCode={StatusCode}, ResponseBody={ResponseBody}",
                (int)response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[DIAG:TwilioSmsSender] HTTP exception during Twilio SMS dispatch for RecipientPhoneMasked={RecipientPhoneMasked}. ExceptionType={ExceptionType}, Message={Message}",
                maskedPhone, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    private static (int charCount, string encoding, int estimatedSegments) AnalyzeMessage(string text)
    {
        int charCount = text.Length;
        bool isGsm7 = IsGsm7(text);
        string encoding = isGsm7 ? "GSM-7" : "UCS-2";

        int segments;
        if (isGsm7)
        {
            segments = charCount <= 160 ? 1 : (int)Math.Ceiling(charCount / 153.0);
        }
        else
        {
            segments = charCount <= 70 ? 1 : (int)Math.Ceiling(charCount / 67.0);
        }

        return (charCount, encoding, segments);
    }

    private static bool IsGsm7(string text)
    {
        foreach (char c in text)
        {
            if (c > 127) return false;
            if (c < 32 && c != '\n' && c != '\r') return false;
        }
        return true;
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "EMPTY";

        var trimmed = phone.Trim();
        if (trimmed.Length <= 6)
            return "***";

        return $"{trimmed[..4]}****{trimmed[^3..]}";
    }
}
