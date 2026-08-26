using System.Text.Json.Serialization;

namespace PropertyOS.Api.UtilityBills;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReplaceUtilityAccountRequest(
    string AccountNumber,
    string? MeterNumber = null);
