using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Api.Companies.Requests;

/// <summary>
/// Request payload for updating company operational settings.
/// </summary>
public record UpdateCompanySettingsRequest(
    short RentGracePeriodDays,
    LateFeeType LateFeeType,
    decimal? LateFeeValue,
    short FiscalYearStartMonth
);
