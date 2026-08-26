using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.Common;

public enum UtilityProviderAvailability
{
    Configured,
    Disabled,
    NotConfigured
}

public sealed record UtilityAccountDto(
    Guid Id,
    UtilityType UtilityType,
    string AccountNumber,
    string? MeterNumber,
    bool IsActive,
    DateOnly? LastKnownBillDate,
    DateTimeOffset? LastSuccessfulSyncAt,
    UtilitySyncStatus SyncStatus,
    short? AverageBillingIntervalDays,
    DateOnly? EstimatedNextBillDate,
    bool HistoricalBootstrapCompleted,
    decimal? TotalOutstandingBalance,
    UtilityBillDto? LatestBill,
    DateTimeOffset? UnlinkedAt = null,
    DateTimeOffset? LinkedAt = null);

/// <summary>
/// Company-scoped management projection. Relationship labels are resolved from
/// the existing lease/property model; provider diagnostics remain internal.
/// </summary>
public sealed record ManagementUtilityAccountDto(
    Guid Id,
    UtilityType UtilityType,
    string AccountNumber,
    string? MeterNumber,
    bool IsActive,
    UtilitySyncStatus SyncStatus,
    UtilityProviderAvailability ProviderAvailability,
    DateOnly? LastKnownBillDate,
    DateTimeOffset? LastSuccessfulSyncAt,
    DateTimeOffset? LastAttemptedSyncAt,
    short ConsecutiveFailureCount,
    bool HistoricalBootstrapCompleted,
    Guid LeaseContractId,
    string LeaseContractNumber,
    Guid TenantId,
    string TenantName,
    Guid ApartmentId,
    string UnitNumber,
    Guid BuildingId,
    string BuildingName,
    UtilityBillDto? LatestBill,
    DateTimeOffset LinkedAt,
    DateTimeOffset? UnlinkedAt = null);

public sealed record UtilityBillDto(
    Guid Id,
    DateOnly BillDate,
    DateOnly? DueDate,
    decimal Amount,
    string Currency,
    bool IsPaid,
    UtilityBillPaymentStatus PaymentStatus,
    DateTimeOffset DiscoveredAt,
    Guid? UtilityAccountId = null,
    string? SourceAccountNumber = null,
    bool? IsCurrentAccount = null,
    UtilityType? SourceUtilityType = null,
    DateTimeOffset? SourceAccountUnlinkedAt = null);

public sealed record UtilityDashboardSummaryDto(
    bool ElectricityLinked,
    UtilityBillDto? LatestElectricityBill,
    bool WaterLinked,
    UtilityBillDto? LatestWaterBill,
    string? ElectricityAccountNumber = null,
    UtilitySyncStatus? ElectricitySyncStatus = null,
    DateTimeOffset? ElectricityLastSuccessfulSyncAt = null,
    string? WaterAccountNumber = null,
    UtilitySyncStatus? WaterSyncStatus = null,
    DateTimeOffset? WaterLastSuccessfulSyncAt = null);
