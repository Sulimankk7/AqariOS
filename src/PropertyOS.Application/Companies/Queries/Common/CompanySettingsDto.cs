using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Application.Companies.Queries.Common;

public class CompanySettingsDto
{
    public string DefaultCurrency { get; set; } = string.Empty;
    public short RentGracePeriodDays { get; set; }
    public LateFeeType LateFeeType { get; set; }
    public decimal? LateFeeValue { get; set; }
    public short FiscalYearStartMonth { get; set; }
    public string DefaultLanguage { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}
