using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Application.Companies.Commands.UpdateCompanySettings;

public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    short RentGracePeriodDays,
    LateFeeType LateFeeType,
    decimal? LateFeeValue,
    short FiscalYearStartMonth
) : ICommand;
