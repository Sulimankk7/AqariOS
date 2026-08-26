using System.Text.Json.Serialization;

namespace PropertyOS.Infrastructure.UtilityBills.Providers.Models;

internal sealed class InternalScraperResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("bills")]
    public List<InternalScraperBill> Bills { get; init; } = [];

    [JsonPropertyName("total_outstanding_balance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? TotalOutstandingBalance { get; init; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }
}

internal sealed class InternalScraperBill
{
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("bill_date")]
    public DateOnly BillDate { get; init; }

    [JsonPropertyName("due_date")]
    public DateOnly? DueDate { get; init; }

    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}
