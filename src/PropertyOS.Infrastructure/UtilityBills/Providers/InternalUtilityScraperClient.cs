using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.UtilityBills.Providers.Models;

namespace PropertyOS.Infrastructure.UtilityBills.Providers;

/// <summary>
/// Authenticated private-network client for the Python scraper service.
/// It sends only the utility account number and maps normalized responses into
/// AqariOS's existing provider-neutral contract.
/// </summary>
public sealed class InternalUtilityScraperClient
{
    private const string TimestampHeader = "X-AqariOS-Timestamp";
    private const string NonceHeader = "X-AqariOS-Nonce";
    private const string SignatureHeader = "X-AqariOS-Signature";

    private readonly HttpClient _httpClient;
    private readonly IOptionsSnapshot<UtilityBillsOptions> _options;
    private readonly ILogger<InternalUtilityScraperClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InternalUtilityScraperClient(
        HttpClient httpClient,
        IOptionsSnapshot<UtilityBillsOptions> options,
        ILogger<InternalUtilityScraperClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured
    {
        get
        {
            var options = _options.Value.ScraperService;
            return Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                   && uri.Scheme is "http" or "https"
                   && Encoding.UTF8.GetByteCount(options.SharedSecret ?? string.Empty) >= 32;
        }
    }

    public async Task<ProviderBillFetchResult> FetchBillsAsync(
        UtilityType utilityType,
        string accountNumber,
        DateOnly? sinceDate,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return ProviderBillFetchResult.Failure(
                "PROVIDER_NOT_CONFIGURED",
                "The internal utility scraper service is not configured.");
        }

        var options = _options.Value.ScraperService;
        var endpoint = utilityType == UtilityType.Electricity
            ? "/internal/v1/electricity/bills"
            : "/internal/v1/water/bills";
        var targetUri = new Uri(new Uri(options.BaseUrl!.TrimEnd('/') + "/"), endpoint.TrimStart('/'));

        var requestBody = JsonSerializer.SerializeToUtf8Bytes(
            new Dictionary<string, string> { ["account_number"] = accountNumber });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        var nonce = Guid.CreateVersion7().ToString("N");
        var signature = ComputeSignature(
            options.SharedSecret!, HttpMethod.Post.Method, targetUri.AbsolutePath,
            timestamp, nonce, requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, targetUri)
        {
            Content = new ByteArrayContent(requestBody)
        };
        request.Content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        request.Headers.Add(TimestampHeader, timestamp);
        request.Headers.Add(NonceHeader, nonce);
        request.Headers.Add(SignatureHeader, signature);

        try
        {
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (responseBytes.Length > options.MaxResponseBytes)
            {
                return ProviderBillFetchResult.Failure(
                    "PARSING_ERROR", "The utility scraper response exceeded the configured size limit.");
            }

            InternalScraperResponse? parsed = null;
            if (responseBytes.Length > 0)
            {
                try
                {
                    parsed = JsonSerializer.Deserialize<InternalScraperResponse>(responseBytes, JsonOptions);
                }
                catch (JsonException)
                {
                    return ProviderBillFetchResult.Failure(
                        "PARSING_ERROR", "The utility scraper returned an invalid response.");
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(parsed?.ErrorCode))
                {
                    return ProviderBillFetchResult.Failure(
                        parsed.ErrorCode, parsed.ErrorMessage ?? "The utility scraper request failed.");
                }

                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                        ProviderBillFetchResult.Failure("AUTH_FAILURE", "Internal scraper authentication failed."),
                    System.Net.HttpStatusCode.TooManyRequests =>
                        ProviderBillFetchResult.Failure("RATE_LIMITED", "The internal scraper is rate limited."),
                    System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.GatewayTimeout =>
                        ProviderBillFetchResult.Failure("TIMEOUT", "The internal scraper request timed out."),
                    _ => ProviderBillFetchResult.Failure(
                        "PROVIDER_UNAVAILABLE", "The internal utility scraper is unavailable.")
                };
            }

            if (parsed is null || !parsed.Success)
            {
                return ProviderBillFetchResult.Failure(
                    parsed?.ErrorCode ?? "PARSING_ERROR",
                    parsed?.ErrorMessage ?? "The utility scraper returned an invalid response.");
            }

            if (parsed.TotalOutstandingBalance < 0)
            {
                return ProviderBillFetchResult.Failure(
                    "PARSING_ERROR", "The utility scraper returned an invalid outstanding balance.");
            }

            var bills = new List<ProviderBillRecord>(parsed.Bills.Count);
            foreach (var item in parsed.Bills)
            {
                if (string.IsNullOrWhiteSpace(item.ExternalId)
                    || item.BillDate == default
                    || item.Amount <= 0)
                {
                    return ProviderBillFetchResult.Failure(
                        "PARSING_ERROR", "The utility scraper returned an invalid bill record.");
                }

                if (sinceDate.HasValue && item.BillDate < sinceDate.Value)
                    continue;

                var paymentStatus = item.Status?.ToUpperInvariant() switch
                {
                    "PAID" => UtilityBillPaymentStatus.Paid,
                    "UNPAID" => UtilityBillPaymentStatus.Unpaid,
                    _ => UtilityBillPaymentStatus.Unknown
                };

                bills.Add(new ProviderBillRecord(
                    item.ExternalId,
                    item.BillDate,
                    item.DueDate,
                    item.Amount,
                    string.IsNullOrWhiteSpace(item.Currency) ? "JOD" : item.Currency.ToUpperInvariant(),
                    paymentStatus == UtilityBillPaymentStatus.Paid,
                    paymentStatus,
                    item.Reference));
            }

            return ProviderBillFetchResult.Success(bills, parsed.TotalOutstandingBalance);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ProviderBillFetchResult.Failure("TIMEOUT", "The utility scraper request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "The internal utility scraper request failed.");
            return ProviderBillFetchResult.Failure(
                "PROVIDER_UNAVAILABLE", "The internal utility scraper is unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected internal utility scraper client failure.");
            return ProviderBillFetchResult.Failure(
                "UNEXPECTED_ERROR", "The internal utility scraper request failed unexpectedly.");
        }
    }

    internal static string ComputeSignature(
        string sharedSecret,
        string method,
        string path,
        string timestamp,
        string nonce,
        ReadOnlySpan<byte> body)
    {
        var bodyHash = Convert.ToHexStringLower(SHA256.HashData(body));
        var canonical = Encoding.UTF8.GetBytes(
            $"{timestamp}\n{nonce}\n{method.ToUpperInvariant()}\n{path}\n{bodyHash}");
        return Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(sharedSecret), canonical));
    }
}
