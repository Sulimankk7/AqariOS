using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.Notifications.Options;

namespace PropertyOS.Infrastructure.Notifications.Services;

/// <summary>
/// Resend HTTP API implementation for AqariOS transactional email.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendOptions _resendOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<ResendOptions> resendOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _resendOptions = resendOptions?.Value ?? throw new ArgumentNullException(nameof(resendOptions));
        _frontendOptions = frontendOptions?.Value ?? throw new ArgumentNullException(nameof(frontendOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resend email sender started. Stage=SenderStarted Recipient={RecipientMasked}",
            MaskEmailForLog(recipientEmail));

        if (!TryNormalizeEmail(recipientEmail, out var normalizedRecipient))
        {
            _logger.LogWarning("Resend email was not sent because the recipient address is invalid. Stage=RecipientValidationFailed");
            return false;
        }

        if (!TryNormalizeEmail(_resendOptions.SenderEmail, out var normalizedSender))
        {
            _logger.LogWarning(
                "Resend email was not sent because sender configuration is missing or invalid. Stage=ConfigurationUnavailable Setting={Setting} Recipient={RecipientMasked}",
                "Resend:SenderEmail",
                MaskEmail(normalizedRecipient));
            return false;
        }

        if (string.IsNullOrWhiteSpace(_resendOptions.ApiKey))
        {
            _logger.LogWarning(
                "Resend email was not sent because API authentication is not configured. Stage=ConfigurationUnavailable Setting={Setting} Recipient={RecipientMasked}",
                "Resend:ApiKey",
                MaskEmail(normalizedRecipient));
            return false;
        }

        if (string.IsNullOrWhiteSpace(subject) ||
            (string.IsNullOrWhiteSpace(htmlBody) && string.IsNullOrWhiteSpace(textBody)))
        {
            _logger.LogWarning(
                "Resend email was not sent because required message fields are empty. Stage=MessageValidationFailed Recipient={RecipientMasked}",
                MaskEmail(normalizedRecipient));
            return false;
        }

        var maskedRecipient = MaskEmail(normalizedRecipient);
        var maskedSender = MaskEmail(normalizedSender);
        _logger.LogInformation(
            "Resend configuration validated. Stage=ConfigurationAvailable ApiKeyConfigured={ApiKeyConfigured} Sender={SenderMasked} Recipient={RecipientMasked}",
            true,
            maskedSender,
            maskedRecipient);

        var senderName = string.IsNullOrWhiteSpace(_resendOptions.SenderName)
            ? "Aqari System"
            : _resendOptions.SenderName.Trim();

        var payload = new ResendEmailRequest
        {
            From = new MailAddress(normalizedSender, senderName).ToString(),
            To = new[] { normalizedRecipient },
            Subject = subject.Trim(),
            Html = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody,
            Text = string.IsNullOrWhiteSpace(textBody) ? null : textBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", _resendOptions.ApiKey.Trim());
        request.Content = JsonContent.Create(payload);

        try
        {
            _logger.LogInformation(
                "Submitting transactional email to Resend. Stage=HttpRequestSending Endpoint={Endpoint} Method={Method} AuthorizationScheme={AuthorizationScheme} Sender={SenderMasked} Recipient={RecipientMasked} SubjectPresent={SubjectPresent} HtmlPresent={HtmlPresent} TextPresent={TextPresent}",
                "/emails",
                HttpMethod.Post.Method,
                "Bearer",
                maskedSender,
                maskedRecipient,
                true,
                payload.Html != null,
                payload.Text != null);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Resend accepted transactional email. Stage=EmailAccepted Recipient={RecipientMasked} StatusCode={StatusCode}",
                    maskedRecipient,
                    (int)response.StatusCode);
                return true;
            }

            var providerError = await ReadSafeErrorAsync(response, cancellationToken);
            _logger.LogWarning(
                "Resend rejected transactional email. Stage=EmailRejected Recipient={RecipientMasked} StatusCode={StatusCode} ErrorType={ErrorType} ErrorDetail={ErrorDetail}",
                maskedRecipient,
                (int)response.StatusCode,
                providerError.Type,
                providerError.Detail);
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Resend request timed out. Stage=HttpTimeout Recipient={RecipientMasked}",
                maskedRecipient);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                "Resend network request failed. Stage=HttpNetworkFailure Recipient={RecipientMasked} ExceptionType={ExceptionType}",
                maskedRecipient,
                ex.GetType().Name);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                "Unexpected Resend email failure. Stage=UnexpectedProviderFailure Recipient={RecipientMasked} ExceptionType={ExceptionType}",
                maskedRecipient,
                ex.GetType().Name);
            return false;
        }
    }

    public Task<bool> SendTenantActivationEmailAsync(
        string recipientEmail,
        string tenantName,
        string activationToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantName) || string.IsNullOrWhiteSpace(activationToken))
        {
            _logger.LogWarning("Tenant activation email was not sent because required data is missing.");
            return Task.FromResult(false);
        }

        var baseUrl = string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl)
            ? "http://localhost:5173"
            : _frontendOptions.BaseUrl.TrimEnd('/');
        var activationUrl = $"{baseUrl}/auth/activate?token={Uri.EscapeDataString(activationToken.Trim())}";

        return SendAsync(
            recipientEmail,
            subject: "تفعيل حسابك في عقاري",
            htmlBody: BuildActivationEmailHtml(tenantName.Trim(), activationUrl),
            textBody: BuildActivationEmailText(tenantName.Trim(), activationUrl),
            cancellationToken: cancellationToken);
    }

    private static async Task<SafeProviderError> ReadSafeErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var errorType = "provider_error";
            if (document.RootElement.TryGetProperty("name", out var nameElement))
            {
                var value = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(value) && value.Length <= 80)
                {
                    var safe = true;
                    foreach (var character in value)
                    {
                        if (!char.IsLetterOrDigit(character) && character is not '_' and not '-')
                        {
                            safe = false;
                            break;
                        }
                    }

                    if (safe)
                        errorType = value;
                }
            }

            var detail = "provider_error";
            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                detail = SanitizeProviderMessage(messageElement.GetString());
            }

            return new SafeProviderError(errorType, detail);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException)
        {
            // The raw response is deliberately not logged or returned.
        }

        return new SafeProviderError("provider_error", "provider_error");
    }

    private static string SanitizeProviderMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "provider_error";

        var sanitized = Regex.Replace(
            message,
            @"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
            match => MaskEmailForLog(match.Value),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        sanitized = Regex.Replace(
            sanitized,
            @"\bre_[A-Za-z0-9_\-]+\b",
            "[REDACTED]",
            RegexOptions.CultureInvariant);
        sanitized = string.Concat(sanitized.Select(character => char.IsControl(character) ? ' ' : character));
        return sanitized.Length <= 300 ? sanitized : sanitized[..300];
    }

    private static bool TryNormalizeEmail(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\r') || value.Contains('\n'))
            return false;

        try
        {
            var address = new MailAddress(value.Trim());
            if (!string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            normalized = address.Address.ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string MaskEmail(string email)
    {
        var separatorIndex = email.IndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == email.Length - 1)
            return "[INVALID]";

        var local = email[..separatorIndex];
        var domain = email[(separatorIndex + 1)..];
        var maskedLocal = local.Length == 1 ? "*" : $"{local[0]}***{local[^1]}";
        var maskedDomain = domain.Length <= 2 ? "**" : $"{domain[0]}***{domain[^1]}";
        return $"{maskedLocal}@{maskedDomain}";
    }

    private static string MaskEmailForLog(string? email) =>
        TryNormalizeEmail(email, out var normalized) ? MaskEmail(normalized) : "[INVALID]";

    private static string BuildActivationEmailText(string tenantName, string activationUrl) =>
        $"مرحباً بك، {tenantName}!{Environment.NewLine}{Environment.NewLine}" +
        "تم إنشاء حساب بوابة المستأجر الخاص بك في عقاري. " +
        "استخدم الرابط التالي لتفعيل الحساب وتعيين كلمة المرور. " +
        $"الرابط صالح لمدة 48 ساعة ويستخدم مرة واحدة فقط.{Environment.NewLine}{Environment.NewLine}" +
        activationUrl;

    private static string BuildActivationEmailHtml(string tenantName, string activationUrl)
    {
        var safeTenantName = System.Net.WebUtility.HtmlEncode(tenantName);
        var safeActivationUrl = System.Net.WebUtility.HtmlEncode(activationUrl);
        var currentYear = DateTime.UtcNow.Year;

        return $@"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>تفعيل حساب بوابة المستأجر | عقاري</title>
</head>
<body style=""margin:0;padding:0;background:#0E1116;color:#F0F3F6;font-family:'Segoe UI',Tahoma,sans-serif;direction:rtl;text-align:right"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""padding:40px 16px;background:#0E1116"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""100%"" style=""max-width:580px;padding:36px 28px;background:#161B22;border:1px solid #21262D;border-radius:12px"">
        <tr><td>
          <h1 style=""margin:0 0 24px;color:#fff;font-size:22px"">عقاري <span style=""color:#A4AC86;font-size:13px"">AqariOS</span></h1>
          <h2 style=""margin:0 0 16px;color:#fff;font-size:19px"">مرحباً بك، {safeTenantName}!</h2>
          <p style=""color:#E5E7EB;line-height:1.8"">تم إنشاء حساب بوابة المستأجر الخاص بك بنجاح في منصة <strong>عقاري</strong>.</p>
          <p style=""color:#9CA3AF;line-height:1.8"">اضغط على الزر أدناه لتفعيل حسابك وتعيين كلمة المرور.</p>
          <div style=""margin:32px 0;text-align:center"">
            <a href=""{safeActivationUrl}"" style=""display:inline-block;padding:14px 36px;background:#414833;color:#fff;text-decoration:none;border:1px solid #656D4A;border-radius:8px;font-weight:700"">تفعيل حسابي</a>
          </div>
          <p style=""color:#9CA3AF;font-size:12px"">أو انسخ الرابط التالي:</p>
          <div style=""padding:12px;background:#0E1116;border:1px solid #21262D;border-radius:8px;color:#A4AC86;direction:ltr;text-align:left;word-break:break-all;font-family:monospace;font-size:11px"">{safeActivationUrl}</div>
          <p style=""margin-top:24px;padding:12px 16px;border-right:3px solid #A4AC86;background:rgba(164,172,134,.08);color:#E5E7EB""><strong>صلاحية الرابط: 48 ساعة</strong><br>يمكن استخدام رابط التفعيل مرة واحدة فقط.</p>
          <p style=""color:#6B7280;font-size:12px"">إذا لم تكن تتوقع هذا البريد، يمكنك تجاهله بأمان.</p>
        </td></tr>
        <tr><td style=""padding-top:20px;border-top:1px solid #21262D;text-align:center;color:#6B7280;font-size:11px"">&copy; {currentYear} عقاري - AqariOS. جميع الحقوق محفوظة.</td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }

    private sealed class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public string From { get; init; } = string.Empty;

        [JsonPropertyName("to")]
        public string[] To { get; init; } = Array.Empty<string>();

        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("html")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Html { get; init; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; init; }
    }

    private sealed record SafeProviderError(string Type, string Detail);
}
