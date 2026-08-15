using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.Notifications.Options;

namespace PropertyOS.Infrastructure.Notifications.Services;

/// <summary>
/// Brevo v3 HTTP transactional email implementation for AqariOS.
/// </summary>
public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoOptions _brevoOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<BrevoOptions> brevoOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _brevoOptions = brevoOptions?.Value ?? throw new ArgumentNullException(nameof(brevoOptions));
        _frontendOptions = frontendOptions?.Value ?? throw new ArgumentNullException(nameof(frontendOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendTenantActivationEmailAsync(
        string recipientEmail,
        string tenantName,
        string activationToken,
        CancellationToken cancellationToken = default)
    {
        var maskedEmail = MaskEmail(recipientEmail);
        _logger.LogInformation(
            "[DIAG:BrevoEmailSender] Email sender invoked. RecipientEmailMasked={RecipientEmailMasked}, TenantName={TenantName}",
            maskedEmail, tenantName);

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("[DIAG:BrevoEmailSender] Cannot send tenant activation email: recipient email is empty.");
            return false;
        }

        bool hasApiKey = !string.IsNullOrWhiteSpace(_brevoOptions.ApiKey);
        var senderEmail = !string.IsNullOrWhiteSpace(_brevoOptions.SenderEmail)
            ? _brevoOptions.SenderEmail.Trim()
            : "aqari.system@gmail.com";

        _logger.LogInformation(
            "[DIAG:BrevoEmailSender] Brevo configuration status: ApiKeyConfigured={HasApiKey}, SenderEmail={SenderEmail}, Endpoint=https://api.brevo.com/v3/smtp/email",
            hasApiKey, senderEmail);

        if (!hasApiKey)
        {
            _logger.LogWarning("[DIAG:BrevoEmailSender] Brevo ApiKey is not configured. Skipping email delivery.");
            return false;
        }

        var baseUrl = _frontendOptions.BaseUrl?.TrimEnd('/') ?? "http://localhost:5173";
        var activationUrl = $"{baseUrl}/auth/activate?token={Uri.EscapeDataString(activationToken.Trim())}";

        var senderName = !string.IsNullOrWhiteSpace(_brevoOptions.SenderName)
            ? _brevoOptions.SenderName.Trim()
            : "عقاري نوت";

        var htmlContent = BuildActivationEmailHtml(tenantName.Trim(), activationUrl);

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = recipientEmail.Trim().ToLowerInvariant(), name = tenantName.Trim() } },
            subject = "تفعيل حسابك في عقاري ",
            htmlContent = htmlContent
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", _brevoOptions.ApiKey.Trim());
        request.Headers.Add("accept", "application/json");
        request.Content = JsonContent.Create(payload);

        try
        {
            _logger.LogInformation("[DIAG:BrevoEmailSender] Dispatching HTTP POST request to Brevo API...");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[DIAG:BrevoEmailSender] Brevo activation email successfully sent to recipient for tenant {TenantName}. HTTP Status={StatusCode} ({(int)response.StatusCode}). ResponseBody={ResponseBody}",
                    tenantName, response.StatusCode, (int)response.StatusCode, responseBody);
                return true;
            }

            _logger.LogError(
                "[DIAG:BrevoEmailSender] Brevo API request failed. HTTP Status={StatusCode} ({(int)response.StatusCode}). ResponseBody={ResponseBody}",
                response.StatusCode, (int)response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[DIAG:BrevoEmailSender] HTTP exception occurred while sending transactional email via Brevo. ExceptionType={ExceptionType}, Message={Message}",
                ex.GetType().Name, ex.Message);
            return false;
        }
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "[EMPTY]";
        var parts = email.Split('@');
        if (parts.Length != 2) return "[INVALID]";
        var name = parts[0];
        var domain = parts[1];
        var maskedName = name.Length <= 2 ? name[0] + "*" : name[0] + "***" + name[^1];
        var maskedDomain = domain.Length <= 4 ? domain : domain[0] + "***" + domain[^1];
        return $"{maskedName}@{maskedDomain}";
    }

    private static string BuildActivationEmailHtml(string tenantName, string activationUrl)
    {
        var currentYear = DateTime.UtcNow.Year;
        var safeTenantName = System.Net.WebUtility.HtmlEncode(tenantName);
        var safeActivationUrl = System.Net.WebUtility.HtmlEncode(activationUrl);

        return $@"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>تفعيل حساب بوابة المستأجر | عقاري نوت</title>
</head>
<body style=""margin:0; padding:0; background-color:#0E1116; font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color:#F0F3F6; direction:rtl; text-align:right;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#0E1116; padding: 40px 16px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""100%"" style=""max-width: 580px; background-color:#161B22; border-radius: 12px; border: 1px solid #21262D; overflow: hidden; padding: 36px 28px; text-align: right;"">
                    <!-- Brand Header -->
                    <tr>
                        <td style=""padding-bottom: 24px; border-bottom: 1px solid #21262D;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td style=""vertical-align: middle;"">
                                        <div style=""display: inline-block; vertical-align: middle; margin-left: 10px;"">
                                            <svg width=""30"" height=""30"" viewBox=""0 0 32 32"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""vertical-align: middle;"">
                                                <rect x=""3""  y=""18"" width=""6"" height=""11"" rx=""1"" fill=""#656D4A"" />
                                                <rect x=""11"" y=""11"" width=""6"" height=""18"" rx=""1"" fill=""#414833"" />
                                                <rect x=""19"" y=""6""  width=""6"" height=""23"" rx=""1"" fill=""#333D29"" />
                                                <circle cx=""27"" cy=""8"" r=""2.5"" fill=""#936639"" />
                                            </svg>
                                        </div>
                                        <span style=""font-size: 22px; font-weight: 700; color: #FFFFFF; vertical-align: middle; letter-spacing: -0.3px;"">
                                            عقاري نوت
                                        </span>
                                        <span style=""font-size: 13px; font-weight: 500; color: #A4AC86; margin-right: 8px; vertical-align: middle;"">
                                            AqariOS
                                        </span>
                                    </td>
                                </tr>
                            </table>
                            <div style=""width: 36px; height: 2px; background-color: #A4AC86; border-radius: 2px; margin-top: 10px;""></div>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding-top: 28px; padding-bottom: 24px; line-height: 1.8; color: #D1D5DB; font-size: 15px;"">
                            <h2 style=""margin: 0 0 16px 0; font-size: 19px; font-weight: 700; color: #FFFFFF;"">
                                مرحباً بك، {safeTenantName}!
                            </h2>
                            <p style=""margin: 0 0 14px 0; color: #E5E7EB;"">
                                تم إنشاء حساب بوابة المستأجر الخاص بك بنجاح في منصة <strong>عقاري نوت</strong>.
                            </p>
                            <p style=""margin: 0 0 28px 0; color: #9CA3AF;"">
                                اضغط على الزر أدناه لتفعيل حسابك وتعيين كلمة المرور لبدء استخدام البوابة.
                            </p>

                            <!-- Primary CTA Button -->
                            <div style=""text-align: center; margin: 32px 0;"">
                                <a href=""{safeActivationUrl}"" target=""_blank"" style=""display: inline-block; background-color: #414833; color: #FFFFFF; text-decoration: none; padding: 14px 36px; border-radius: 8px; font-weight: 700; font-size: 15px; border: 1px solid #656D4A; box-shadow: 0 4px 14px rgba(65, 72, 51, 0.4); text-align: center;"">
                                    تفعيل حسابي
                                </a>
                            </div>

                            <!-- Manual Link Fallback -->
                            <p style=""margin: 0 0 8px 0; font-size: 12.5px; color: #9CA3AF;"">
                                أو يمكنك نسخ رابط التفعيل ولصقه مباشرة في المتصفح:
                            </p>
                            <div style=""background-color: #0E1116; border: 1px solid #21262D; padding: 12px 14px; border-radius: 8px; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace; font-size: 11.5px; color: #A4AC86; word-break: break-all; text-align: left; direction: ltr; margin-bottom: 24px;"">
                                {safeActivationUrl}
                            </div>

                            <!-- Validity Notice Box -->
                            <div style=""background-color: rgba(164, 172, 134, 0.08); border-right: 3px solid #A4AC86; padding: 12px 16px; border-radius: 6px; margin-bottom: 24px; font-size: 13px; color: #E5E7EB; line-height: 1.6;"">
                                <strong style=""color: #FFFFFF;"">صلاحية الرابط: 48 ساعة</strong>
                                <br/>
                                <span style=""color: #A4AC86;"">يمكن استخدام رابط التفعيل مرة واحدة فقط.</span>
                            </div>

                            <p style=""margin: 0; font-size: 12px; color: #6B7280;"">
                                إذا لم تكن تتوقع هذا البريد أو لم تطلب تفعيل الحساب، يمكنك تجاهل هذه الرسالة بأمان.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""padding-top: 20px; border-top: 1px solid #21262D; text-align: center; font-size: 11.5px; color: #6B7280;"">
                            &copy; {currentYear} عقاري نوت - AqariOS. جميع الحقوق محفوظة.
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
