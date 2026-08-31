using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenant;

public record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string NationalId,
    string Phone,
    string? Email = null,
    string? Occupation = null,
    string? Employer = null,
    string? PhoneCountryCode = "JO"
) : ICommand;
